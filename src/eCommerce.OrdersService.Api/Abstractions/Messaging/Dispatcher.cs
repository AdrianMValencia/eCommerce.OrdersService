using eCommerce.OrdersService.Api.Shared.Bases;

namespace eCommerce.OrdersService.Api.Abstractions.Messaging;

public class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<BaseResponse<TResponse>> Dispatch<TRequest, TResponse>(TRequest request,
        CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        try
        {
            if (request is ICommand<TResponse>)
            {
                var handleType = typeof(ICommandHandler<,>)
                    .MakeGenericType(request.GetType(), typeof(TResponse));

                dynamic handler = _serviceProvider.GetRequiredService(handleType);

                return await handler.Handle((dynamic)request, cancellationToken);
            }

            if (request is IQuery<TResponse>)
            {
                var handleType = typeof(IQueryHandler<,>)
                    .MakeGenericType(request.GetType(), typeof(TResponse));

                dynamic handler = _serviceProvider.GetRequiredService(handleType);

                return await handler.Handle((dynamic)request, cancellationToken);
            }

            throw new InvalidOperationException("Tipo de solicitud no compatible.");
        }
        catch (Exception ex)
        {
            return new BaseResponse<TResponse>
            {
                IsSuccess = false,
                Message = "Ocurrió un error al procesar la solicitud.",
                Errors =
                [
                    new() { PropertyName = "Dispatcher", ErrorMessage = ex.Message }
                ]
            };
        }
    }
}
