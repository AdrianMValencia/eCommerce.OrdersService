using Carter;
using eCommerce.OrdersService.Api.Abstractions.Messaging;
using eCommerce.OrdersService.Api.Contracts.Orders;
using eCommerce.OrdersService.Api.Entities;
using eCommerce.OrdersService.Api.Shared.Bases;
using Mapster;
using MongoDB.Driver;

namespace eCommerce.OrdersService.Api.Features.Orders;

public class GetByIdOrder
{
    #region Query
    public sealed class Query : IQuery<OrderResponse>
    {
        public Guid OrderID { get; set; }
    }
    #endregion

    #region Handler
    internal sealed class Handler : IQueryHandler<Query, OrderResponse>
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly string collectionName = "orders";

        public Handler(IMongoDatabase mongoDatabase)
        {
            _orders = mongoDatabase.GetCollection<Order>(collectionName);
        }

        public async Task<BaseResponse<OrderResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<OrderResponse>();

            try
            {
                var filter =
                    Builders<Order>.Filter
                        .Eq(o => o.OrderID, query.OrderID);

                var order = await _orders
                    .Find(filter)
                    .FirstOrDefaultAsync(cancellationToken);

                var orderResponse = order.Adapt<OrderResponse>();

                response.IsSuccess = true;
                response.Data = orderResponse;
                response.Message = "Order retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"An error occurred while retrieving the order. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class GetByIdOrderEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/orders/{orderId}", async (
                Guid orderId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var query = new Query { OrderID = orderId };
                
                var response = await dispatcher
                    .Dispatch<Query, OrderResponse>(query, cancellationToken);

                return Results.Ok(response);
            });
        }
    }
    #endregion
}
