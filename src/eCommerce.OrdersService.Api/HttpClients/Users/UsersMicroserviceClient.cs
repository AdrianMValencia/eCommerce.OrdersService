using eCommerce.OrdersService.Api.Contracts.Users;
using System.Net;

namespace eCommerce.OrdersService.Api.HttpClients.Users;

public class UsersMicroserviceClient(HttpClient httpClient) : IUsersMicroserviceClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<UserResponse?> GetUserByUserId(Guid userID, CancellationToken cancellationToken)
    {
        using var response = await _httpClient
            .GetAsync($"/api/users/{userID}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new Exception(
                $"Error retrieving user with ID {userID}. Status code: {response.StatusCode}");

        var user = await response.Content
            .ReadFromJsonAsync<UserResponse>(cancellationToken: cancellationToken);

        return user;
    }
}
