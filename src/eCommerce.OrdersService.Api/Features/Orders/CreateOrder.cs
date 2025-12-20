using Carter;
using eCommerce.OrdersService.Api.Abstractions.Messaging;
using eCommerce.OrdersService.Api.Entities;
using eCommerce.OrdersService.Api.HttpClients;
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

        public Handler(IMongoDatabase mongoDatabase, IUsersMicroserviceClient usersMicroserviceClient)
        {
            _orders = mongoDatabase.GetCollection<Order>(collectionName);
            _usersMicroserviceClient = usersMicroserviceClient;
        }

        public async Task<BaseResponse<bool>> Handle(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            try
            {
                var user = await _usersMicroserviceClient
                    .GetUserByUserId(command.UserID, cancellationToken);

                if (user is null)
                {
                    response.IsSuccess = false;
                    response.Message = "User not found.";
                    return response;
                }

                var order = command.Adapt<Order>();
                order.OrderID = Guid.NewGuid();
                order._id = order.OrderID;

                foreach(OrderItem orderItem in order.OrderItems)
                {
                    orderItem._id = Guid.NewGuid();
                    orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
                }

                order.OrderDate = DateTime.UtcNow;
                order.TotalBill = order.OrderItems.Sum(oi => oi.TotalPrice);

                await _orders.InsertOneAsync(order, cancellationToken: cancellationToken);

                response.IsSuccess = true;
                response.Message = "Order created successfully.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"An error occurred while creating the order. {ex.Message}";
            }

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
