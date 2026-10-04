using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Resilience;

public static class StandardHttpPolicies
{
    private const int RetryCount = 3;
    private const int FailuresBeforeBreaking = 5;
    private const double FailureRatio = 0.5;
    private static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SamplingDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TotalRequestTimeout = TimeSpan.FromSeconds(100);

    public static void Configure(HttpStandardResilienceOptions options, IServiceProvider serviceProvider)
    {
        ILogger logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(StandardHttpPolicies).FullName!);

        options.TotalRequestTimeout.Timeout = TotalRequestTimeout;
        options.AttemptTimeout.Timeout = AttemptTimeout;
        options.Retry.MaxRetryAttempts = RetryCount;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.Delay = TimeSpan.FromSeconds(2);
        options.Retry.UseJitter = true;
        options.Retry.OnRetry = arguments =>
        {
            logger.LogWarning(
                "Retrying an outbound call in {DelaySeconds}s, attempt {Attempt} of {RetryCount}: {Reason}",
                arguments.RetryDelay.TotalSeconds, arguments.AttemptNumber + 1, RetryCount,
                Describe(arguments.Outcome));

            return default;
        };

        options.CircuitBreaker.FailureRatio = FailureRatio;
        options.CircuitBreaker.MinimumThroughput = FailuresBeforeBreaking;
        options.CircuitBreaker.SamplingDuration = SamplingDuration;
        options.CircuitBreaker.BreakDuration = BreakDuration;
        options.CircuitBreaker.OnOpened = arguments =>
        {
            logger.LogError(
                "Circuit opened for {DelaySeconds}s after {Failures} transient failures: {Reason}",
                arguments.BreakDuration.TotalSeconds, FailuresBeforeBreaking, Describe(arguments.Outcome));

            return default;
        };
        options.CircuitBreaker.OnClosed = _ =>
        {
            logger.LogInformation("Circuit reset; outbound calls are flowing again.");

            return default;
        };
        options.CircuitBreaker.OnHalfOpened = _ =>
        {
            logger.LogInformation("Circuit half-open; the next call is a trial.");

            return default;
        };
    }

    /// <summary>
    /// A handled 5xx or 408 carries **no exception**, so a callback that read only
    /// <c>Outcome.Exception?.Message</c> would log an empty reason for the most common
    /// trigger. This reports the status code in that case.
    /// </summary>
    private static string Describe(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Exception is not null)
        {
            return outcome.Exception.Message;
        }

        return outcome.Result is null
            ? "no response"
            : $"HTTP {(int)outcome.Result.StatusCode} {outcome.Result.StatusCode}";
    }
}
