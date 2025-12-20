using Carter;
using eCommerce.OrdersService.Api.Abstractions.Messaging;
using eCommerce.OrdersService.Api.Entities;
using eCommerce.OrdersService.Api.Shared.Bases;
using MongoDB.Driver;

namespace eCommerce.OrdersService.Api.Features.Orders;

public class DeleteOrder
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
        public Guid OrderID { get; set; }
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

                var result = await _orders.DeleteOneAsync(filter, cancellationToken);

                if (result.DeletedCount == 0)
                {
                    response.IsSuccess = false;
                    response.Message = "Order not found.";
                    return response;
                }

                response.IsSuccess = true;
                response.Message = "Order deleted successfully.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"An error occurred while deleting the order. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class DeleteOrderEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/orders/remove/{orderId}", async(
                Guid orderId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken
                ) =>
            {
                var command = new Command { OrderID = orderId };

                var response = await dispatcher.Dispatch<Command, bool>(command, cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
