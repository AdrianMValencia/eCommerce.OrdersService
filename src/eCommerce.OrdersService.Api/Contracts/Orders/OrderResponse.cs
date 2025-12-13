namespace eCommerce.OrdersService.Api.Contracts.Orders;  
    
public class OrderResponse
{
    public Guid OrderID { get; set; }
    public Guid UserID { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalBill { get; set; }
    public List<OrderItemResponse> OrderItems { get; set; } = [];
}
