using eCommerce.OrdersService.Api.Contracts.Users;

namespace eCommerce.OrdersService.Api.HttpClients.Users;

public interface IUsersMicroserviceClient
{
    Task<UserResponse?> GetUserByUserId(Guid userID, CancellationToken cancellationToken); 
}
