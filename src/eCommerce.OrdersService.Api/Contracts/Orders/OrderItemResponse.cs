namespace eCommerce.OrdersService.Api.Contracts.Orders;

public class OrderItemResponse
{
    public Guid ProductID { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}
