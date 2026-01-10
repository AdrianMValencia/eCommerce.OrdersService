namespace eCommerce.OrdersService.Api.Features.Products.Consumers;

public interface IProductNameUpdatedConsumer
{
    Task ConsumeAsync(CancellationToken cancellationToken);
    void Dispose();
}
