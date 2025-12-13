using Carter;
using eCommerce.OrdersService.Api.Abstractions.Messaging;
using eCommerce.OrdersService.Api.Contracts.Orders;
using eCommerce.OrdersService.Api.Entities;
using eCommerce.OrdersService.Api.Shared.Bases;
using Mapster;
using MongoDB.Driver;

namespace eCommerce.OrdersService.Api.Features.Orders;

public class GetAllOrders
{
    #region Query
    public sealed class Query : IQuery<IEnumerable<OrderResponse>>
    {

    }
    #endregion

    #region Handler
    internal sealed class Handler : IQueryHandler<Query, IEnumerable<OrderResponse>>
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly string collectionName = "orders";

        public Handler(IMongoDatabase mongoDatabase)
        {
            _orders = mongoDatabase.GetCollection<Order>(collectionName);
        }

        public async Task<BaseResponse<IEnumerable<OrderResponse>>> Handle(Query query, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<IEnumerable<OrderResponse>>();

            try
            {
                var orders = await _orders
                    .Find(_ => true)
                    .ToListAsync(cancellationToken);

                var orderResponse = orders.Adapt<IEnumerable<OrderResponse>>();

                response.IsSuccess = true;
                response.Data = orderResponse;
                response.Message = "Orders retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class GetAllOrderEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/orders", async (IDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var query = new Query();
                var result = await dispatcher.Dispatch<Query, IEnumerable<OrderResponse>>(query, cancellationToken);
                return Results.Ok(result);
            });
        }
    }
    #endregion
}
