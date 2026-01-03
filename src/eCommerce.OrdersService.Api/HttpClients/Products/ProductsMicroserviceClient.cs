using eCommerce.OrdersService.Api.Contracts.Products;
using eCommerce.OrdersService.Api.Contracts.Users;
using eCommerce.OrdersService.Api.Shared.Bases;
using System.Net;

namespace eCommerce.OrdersService.Api.HttpClients.Products;

public class ProductsMicroserviceClient(HttpClient httpClient) : IProductsMicroserviceClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<BaseResponse<GetAllProductsResponseDto>?> GetProductByProductId(Guid productID, CancellationToken cancellationToken)
    {
        using var response = await _httpClient
            .GetAsync($"/api/product/{productID}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new Exception(
                $"Error retrieving product with ID {productID}. Status code: {response.StatusCode}");

        var product = await response.Content
            .ReadFromJsonAsync<BaseResponse<GetAllProductsResponseDto>>(cancellationToken: cancellationToken);

        return product;
    }
}
