using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System;

namespace eCommerce.OrdersService.Api.Shared.Policies.Users;

public class UsersMicroservicePolicy(
    ILogger<UsersMicroservicePolicy> logger) : IUsersMicroservicePolicy
{
    private readonly ILogger<UsersMicroservicePolicy> _logger = logger;

    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        AsyncCircuitBreakerPolicy<HttpResponseMessage> policy =
            Policy
                .HandleResult<HttpResponseMessage>(response => (int)response.StatusCode >= 500)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromMinutes(2),
                    onBreak: (outcome, timespan) =>
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation(
                                "Circuit Breaker se abrió durante {DelayMinutes} minutos debido a 3 fallos consecutivos." +
                                "Las solicitudes subsiguientes se bloquearán.",
                                timespan.TotalMinutes
                                );
                        }
                    },
                    onReset: () =>
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation(
                                "Circuit Breaker cerrado. Se permitirán las solicitudes subsiguientes."
                                );
                        }
                    }
                );

        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        AsyncRetryPolicy<HttpResponseMessage> policy =
            Policy
                .HandleResult<HttpResponseMessage>(response => (int)response.StatusCode >= 500)
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
                        TimeSpan.FromMicroseconds(Random.Shared.Next(0, 500)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation(
                                "Reitentar {RetryAttempt} después de {DelaySeconds} segundos",
                                retryAttempt,
                                timespan.TotalSeconds
                                );
                        }
                    }
                );

        return policy;
    }
}
