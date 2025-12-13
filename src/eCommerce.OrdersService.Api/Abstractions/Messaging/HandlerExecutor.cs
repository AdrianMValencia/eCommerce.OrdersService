using eCommerce.OrdersService.Api.Shared.Bases;
using eCommerce.OrdersService.Api.Shared.Behaviors;
using eCommerce.OrdersService.Api.Shared.Exceptions;

namespace eCommerce.OrdersService.Api.Abstractions.Messaging;

public class HandlerExecutor(IValidationService validationService,
    ILogger<HandlerExecutor> logger)
{
    private readonly IValidationService _validationService = validationService;
    private readonly ILogger<HandlerExecutor> _logger = logger;

    public async Task<BaseResponse<T>> ExecuteAsync<TRequest, T>(
        TRequest request,
        Func<Task<BaseResponse<T>>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            await _validationService.ValidateAsync(request, cancellationToken);

            return await action();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(
                "Validation failed for request of type {@Request}. Errors: {@Errors}",
                request,
                ex.Errors);

            return new BaseResponse<T>
            {
                IsSuccess = false,
                Message = "Errores de validación",
                Errors = ex.Errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while processing request of type {@Request}",
                request);

            return new BaseResponse<T>
            {
                IsSuccess = false,
                Message = "Ocurrió un error inesperado",
                Errors =
                [
                    new() { PropertyName = "Exception", ErrorMessage = ex.Message  }
                ]
            };
        }
    }
}
