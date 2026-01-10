using eCommerce.OrdersService.Api.Contracts.IntegrationEvents.Products;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace eCommerce.OrdersService.Api.Features.Products.Consumers;

public class ProductNameUpdatedConsumer : IProductNameUpdatedConsumer, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IChannel _channel;
    private readonly IConnection _connection;
    private readonly ILogger<ProductNameUpdatedConsumer> _logger;

    public ProductNameUpdatedConsumer(
        IConfiguration configuration,
        ILogger<ProductNameUpdatedConsumer> logger)
    {
        _configuration = configuration;

        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RABBITMQ_HOST"]!,
            UserName = _configuration["RABBITMQ_USER"]!,
            Password = _configuration["RABBITMQ_PASSWORD"]!,
            Port = int.Parse(_configuration["RABBITMQ_PORT"]!)
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _logger = logger;
    }

    public async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        string routingKey = "product.update.name";
        string queueName = "orders.product.update.name.queue";
        string exchangeName = _configuration["RABBITMQ_Products_Exchange"]!;

        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, args) =>
        {
            byte[] body = args.Body.ToArray();

            string message = Encoding.UTF8.GetString(body);

            if (message is not null)
            {
                var productNameUpdatedEvent =
                    JsonSerializer.Deserialize<ProductNameUpdatedEvent>(message);

                _logger.LogInformation("Product name updated: {ProductID}, NewName: {NewName}",
                    productNameUpdatedEvent?.ProductID,
                    productNameUpdatedEvent?.NewName);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            consumer: consumer,
            autoAck: true,
            cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
