using Carter;
using eCommerce.OrdersService.Api.Abstractions.Messaging;
using eCommerce.OrdersService.Api.Contracts.Orders;
using eCommerce.OrdersService.Api.Contracts.Products;
using eCommerce.OrdersService.Api.Entities;
using eCommerce.OrdersService.Api.HttpClients.Products;
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
        private readonly IProductsMicroserviceClient _productsMicroserviceClient;


        public Handler(IMongoDatabase mongoDatabase, 
            IProductsMicroserviceClient productsMicroserviceClient)
        {
            _orders = mongoDatabase.GetCollection<Order>(collectionName);
            _productsMicroserviceClient = productsMicroserviceClient;
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

                if (order is null)
                {
                    response.IsSuccess = false;
                    response.Message = "La orden no existe.";
                    return response;
                }

                var orderResponse = order.Adapt<OrderResponse>();

                var productCache = new Dictionary<Guid, GetAllProductsResponseDto>();

                foreach (var item in orderResponse.OrderItems)
                {
                    if (!productCache.TryGetValue(item.ProductID, out var product))
                    {
                        var productResponse = await _productsMicroserviceClient
                            .GetProductByProductId(item.ProductID, cancellationToken);

                        if (productResponse?.Data is null)
                            continue;

                        product = productResponse.Data;
                        productCache[item.ProductID] = product;
                    }
                }

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
