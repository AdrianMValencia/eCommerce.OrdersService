using eCommerce.OrdersService.Api.Contracts.Products;
using eCommerce.OrdersService.Api.Shared.Bases;
using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using System.Text.Json;

namespace eCommerce.OrdersService.Api.HttpClients.Products;

public class ProductsMicroserviceClient(
    HttpClient httpClient,
    IDistributedCache distributedCache) : IProductsMicroserviceClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IDistributedCache _distributedCache = distributedCache;

    public async Task<BaseResponse<GetAllProductsResponseDto>?> GetProductByProductId(Guid productID, CancellationToken cancellationToken)
    {
        string cachekey = $"product: {productID}";

        string? cacheProduct = await _distributedCache.GetStringAsync(cachekey, cancellationToken);

        var productFromCache = new BaseResponse<GetAllProductsResponseDto>();

        if (cacheProduct is not null)
        {
            var productDataCache = JsonSerializer.Deserialize<GetAllProductsResponseDto>(cacheProduct);
            productFromCache.Data = productDataCache;

            return productFromCache;
        }

        using var response = await _httpClient
            .GetAsync($"/api/product/{productID}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new Exception(
                $"Error retrieving product with ID {productID}. Status code: {response.StatusCode}");

        var product = await response.Content
            .ReadFromJsonAsync<BaseResponse<GetAllProductsResponseDto>>(cancellationToken: cancellationToken);

        string productJson = JsonSerializer.Serialize(product?.Data);

        var cacheOptions = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(300))
            .SetSlidingExpiration(TimeSpan.FromSeconds(100));

        string cachekeyToWrite = $"product: {productID}";

        await _distributedCache.SetStringAsync(
            cachekeyToWrite,
            productJson,
            cacheOptions,
            cancellationToken);

        return product;
    }
}
