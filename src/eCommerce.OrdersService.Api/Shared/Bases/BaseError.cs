namespace eCommerce.OrdersService.Api.Shared.Bases;

public class BaseError
{
    public string? PropertyName { get; set; }
    public string? ErrorMessage { get; set; }
}
