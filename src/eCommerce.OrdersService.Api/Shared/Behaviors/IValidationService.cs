namespace eCommerce.OrdersService.Api.Shared.Behaviors;

public interface IValidationService
{
    Task ValidateAsync<T>(T request, CancellationToken cancellationToken = default);
}
