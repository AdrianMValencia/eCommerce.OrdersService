namespace eCommerce.OrdersService.Api.Contracts.Users;

public class UserResponse
{
    public Guid UserID { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
}
