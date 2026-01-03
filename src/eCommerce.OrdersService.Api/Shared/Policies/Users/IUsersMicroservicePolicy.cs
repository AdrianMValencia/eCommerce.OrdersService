using Polly;

namespace eCommerce.OrdersService.Api.Shared.Policies.Users;

public interface IUsersMicroservicePolicy
{
    IAsyncPolicy<HttpResponseMessage> GetRetryPolicy();
    IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy();
}
