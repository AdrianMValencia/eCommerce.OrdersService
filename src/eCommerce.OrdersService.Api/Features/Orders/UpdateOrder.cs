using Carter;
using eCommerce.OrdersService.Api.Abstractions.Messaging;
using eCommerce.OrdersService.Api.Entities;
using eCommerce.OrdersService.Api.Shared.Bases;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace eCommerce.OrdersService.Api.Features.Orders;

public class UpdateOrder
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
        public Guid OrderID { get; set; }
        public Guid UserID { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItemCommand> OrderItems { get; set; } = [];
    }

    public sealed class OrderItemCommand
    {
        public Guid ProductID { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
    #endregion

    #region Handler
    internal sealed class Handler : ICommandHandler<Command, bool>
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly string collectionName = "orders";

        public Handler(IMongoDatabase mongoDatabase)
        {
            _orders = mongoDatabase.GetCollection<Order>(collectionName);
        }

        public async Task<BaseResponse<bool>> Handle(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            try
            {
                var filter = Builders<Order>.Filter
                .Eq(o => o.OrderID, command.OrderID);

                var existingOrder = await _orders
                    .Find(filter)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingOrder is null)
                {
                    response.IsSuccess = false;
                    response.Message = "Order not found.";
                    return response;
                }

                // Validar si el usuario existe : UsersMicroservice

                var updatedOrder = command.Adapt<Order>();
                updatedOrder._id = existingOrder._id;
                updatedOrder.OrderID = existingOrder.OrderID;

                foreach (OrderItem orderItem in updatedOrder.OrderItems)
                {
                    orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
                }

                updatedOrder.TotalBill = updatedOrder.OrderItems.Sum(oi => oi.TotalPrice);

                await _orders.ReplaceOneAsync(
                    filter,
                    updatedOrder,
                    cancellationToken: cancellationToken);

                response.IsSuccess = true;
                response.Message = "Order updated successfully.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"An error occurred while updating the order. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class UpdateOrderEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("api/orders/edit", async(
                Command command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken
                ) =>
            {
                var response = await dispatcher.Dispatch<Command, bool>(command, cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
