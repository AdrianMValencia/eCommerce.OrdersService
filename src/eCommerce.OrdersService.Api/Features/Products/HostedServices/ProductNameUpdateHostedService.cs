
using eCommerce.OrdersService.Api.Features.Products.Consumers;

namespace eCommerce.OrdersService.Api.Features.Products.HostedServices;

public class ProductNameUpdateHostedService : IHostedService
{
    private readonly IProductNameUpdatedConsumer _productNameUpdatedConsumer;

    public ProductNameUpdateHostedService(
        IProductNameUpdatedConsumer productNameUpdatedConsumer)
    {
        _productNameUpdatedConsumer = productNameUpdatedConsumer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _productNameUpdatedConsumer.ConsumeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _productNameUpdatedConsumer.Dispose();
        return Task.CompletedTask;
    }
}
