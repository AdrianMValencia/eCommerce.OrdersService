using Carter;
using eCommerce.OrdersService.Api.Abstractions.Messaging;
using eCommerce.OrdersService.Api.Entities;
using eCommerce.OrdersService.Api.HttpClients.Products;
using eCommerce.OrdersService.Api.HttpClients.Users;
using eCommerce.OrdersService.Api.Shared.Bases;
using FluentValidation;
using Mapster;
using MongoDB.Driver;

namespace eCommerce.OrdersService.Api.Features.Orders;

public class CreateOrder
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
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

    #region Validator
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserID)
                .NotEmpty().WithMessage("UserID is required.");

            RuleFor(x => x.OrderDate)
                .NotEmpty().WithMessage("OrderDate is required.");

            RuleFor(x => x.OrderItems)
                .NotEmpty().WithMessage("At least one order item is required.");
        }
    }

    public class OrderItemCommandValidator : AbstractValidator<OrderItemCommand>
    {
        public OrderItemCommandValidator()
        {
            RuleFor(x => x.ProductID)
            .NotEmpty().WithMessage("ProductID is required.");

            RuleFor(x => x.UnitPrice)
                .NotEmpty().WithMessage("UnitPrice is required.")
                .GreaterThan(0).WithMessage("UnitPrice must be greater than zero.");

            RuleFor(x => x.Quantity)
                .NotEmpty().WithMessage("Quantity is required.")
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        }
    }
    #endregion

    #region Handler
    internal sealed class Handler : ICommandHandler<Command, bool>
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly string collectionName = "orders";
        private readonly IUsersMicroserviceClient _usersMicroserviceClient;
        private readonly IProductsMicroserviceClient _productsMicroserviceClient;

        public Handler(IMongoDatabase mongoDatabase, IUsersMicroserviceClient usersMicroserviceClient, IProductsMicroserviceClient productsMicroserviceClient)
        {
            _orders = mongoDatabase.GetCollection<Order>(collectionName);
            _usersMicroserviceClient = usersMicroserviceClient;
            _productsMicroserviceClient = productsMicroserviceClient;
        }

        public async Task<BaseResponse<bool>> Handle(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            try
            {
                if (!await UserExists(command.UserID, cancellationToken))
                    return Fail(response, "El usuario no existe.");

                var invalidProductId = await GetInvalidProductId(command, cancellationToken);
                if (invalidProductId is not null)
                    return Fail(response, $"El producto con ID '{invalidProductId}' no existe.");

                var order = BuildOrder(command);

                await _orders.InsertOneAsync(order, cancellationToken: cancellationToken);

                response.IsSuccess = true;
                response.Message = "Se registró correctamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al registrar la orden: {ex.Message}";
            }

            return response;
        }

        private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
            => await _usersMicroserviceClient
                .GetUserByUserId(userId, cancellationToken) is not null;

        private async Task<Guid?> GetInvalidProductId(
            Command command,
            CancellationToken cancellationToken)
        {
            var productIds = command.OrderItems
                .Select(x => x.ProductID)
                .Distinct();

            foreach (var productId in productIds)
            {
                var product = await _productsMicroserviceClient
                    .GetProductByProductId(productId, cancellationToken);

                if (product is null)
                    return productId;
            }

            return null;
        }

        private static Order BuildOrder(Command command)
        {
            var order = command.Adapt<Order>();

            order.OrderID = Guid.NewGuid();
            order._id = order.OrderID;
            order.OrderDate = DateTime.UtcNow;

            foreach (var item in order.OrderItems)
            {
                item._id = Guid.NewGuid();
                item.TotalPrice = item.Quantity * item.UnitPrice;
            }

            order.TotalBill = order.OrderItems.Sum(x => x.TotalPrice);

            return order;
        }

        private static BaseResponse<bool> Fail(BaseResponse<bool> response, string message)
        {
            response.IsSuccess = false;
            response.Message = message;
            return response;
        }
    }
    #endregion

    #region Endpoint
    public class CreateOrderEnpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/orders/register", async (
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
