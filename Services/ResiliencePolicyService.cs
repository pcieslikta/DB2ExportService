using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using DB2ExportService.Configuration;
using Microsoft.Extensions.Logging;

namespace DB2ExportService.Services;

/// <summary>
/// Serwis zarządzający politykami resilience (Polly) - retry, circuit breaker
/// </summary>
public class ResiliencePolicyService
{
    private readonly ILogger<ResiliencePolicyService> _logger;
    private readonly ResiliencePipeline _dbPipeline;

    public ResiliencePolicyService(ILogger<ResiliencePolicyService> logger, ConfigurationHelper configHelper)
    {
        _logger = logger;
        var config = configHelper.GetExportConfig();

        _dbPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = config.RetryCount,
                Delay = TimeSpan.FromSeconds(config.RetryDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("DB Retry {AttemptNumber}/{MaxRetries} po błędzie: {Exception}",
                        args.AttemptNumber, config.RetryCount, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(config.CircuitBreakerDurationSeconds),
                MinimumThroughput = config.CircuitBreakerFailureThreshold,
                BreakDuration = TimeSpan.FromSeconds(config.CircuitBreakerDurationSeconds),
                OnOpened = args =>
                {
                    _logger.LogError("Circuit Breaker OTWARTY dla DB2! Zbyt wiele błędów. Wyłączenie na {Duration}s",
                        config.CircuitBreakerDurationSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit Breaker ZAMKNIĘTY dla DB2. Połączenie przywrócone.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Circuit Breaker w trybie HALF-OPEN. Testowanie połączenia...");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Wykonuje operację bazodanową z retry i circuit breaker
    /// </summary>
    public async Task<T> ExecuteDbOperationAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            _logger.LogDebug("Wykonywanie operacji DB: {OperationName}", operationName);

            return await _dbPipeline.ExecuteAsync(async ct =>
            {
                return await operation();
            });
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError("Circuit Breaker OTWARTY - operacja {OperationName} odrzucona", operationName);
            throw new InvalidOperationException($"Połączenie z bazą danych tymczasowo niedostępne: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd operacji DB {OperationName} po wszystkich retry", operationName);
            throw;
        }
    }

    /// <summary>
    /// Wykonuje operację bazodanową void z retry i circuit breaker
    /// </summary>
    public async Task ExecuteDbOperationAsync(Func<Task> operation, string operationName)
    {
        await ExecuteDbOperationAsync(async () =>
        {
            await operation();
            return true;
        }, operationName);
    }
}
