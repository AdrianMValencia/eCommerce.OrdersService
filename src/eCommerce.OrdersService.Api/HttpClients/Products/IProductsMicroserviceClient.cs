using eCommerce.OrdersService.Api.Contracts.Products;
using eCommerce.OrdersService.Api.Shared.Bases;

namespace eCommerce.OrdersService.Api.HttpClients.Products;

public interface IProductsMicroserviceClient
{
    Task<BaseResponse<GetAllProductsResponseDto>?> GetProductByProductId(Guid productID, CancellationToken cancellationToken);
}
