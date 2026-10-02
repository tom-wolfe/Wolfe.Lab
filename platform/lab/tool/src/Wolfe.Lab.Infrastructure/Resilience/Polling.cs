using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Wolfe.Lab.Infrastructure.Resilience;

/// <summary>
/// The lab's one way to wait for something: ask again at an interval until the answer is yes,
/// and give up after a limit.
/// </summary>
/// <remarks>
/// A resilience pipeline rather than a loop in each step, so every wait reads the same, reports its
/// retries and timeouts through the same telemetry, and runs on the injected
/// <see cref="TimeProvider"/> — a test waits no real time. Registered by each client beside the
/// thing it waits on, and resolved by key.
/// </remarks>
public static class Polling
{
    /// <summary>
    /// The limit for one wait, when it is not the pipeline's own: an agent's exit timeout.
    /// </summary>
    private static readonly ResiliencePropertyKey<TimeSpan> Limit = new("lab.polling.limit");

    extension(ResiliencePipelineBuilder<bool> builder)
    {
        /// <summary>
        /// Gives up after <paramref name="limit"/> — or the <see cref="Limit"/> a caller puts in the
        /// context — and within it, asks again every <paramref name="interval"/> while the answer is no.
        /// </summary>
        public ResiliencePipelineBuilder<bool> AddPolling(TimeSpan limit, TimeSpan interval) => builder
            .AddTimeout(new TimeoutStrategyOptions
            {
                TimeoutGenerator = args => ValueTask.FromResult(args.Context.Properties.GetValue(Limit, limit))
            })
            .AddRetry(new RetryStrategyOptions<bool>
            {
                ShouldHandle = new PredicateBuilder<bool>().HandleResult(false),
                BackoffType = DelayBackoffType.Constant,
                Delay = interval,
                UseJitter = false,
                MaxRetryAttempts = int.MaxValue
            });
    }

    extension(ResiliencePipeline<bool> pipeline)
    {
        /// <summary>
        /// Whether the condition came true within the limit.
        /// </summary>
        /// <param name="condition">The question, asked until it answers yes.</param>
        /// <param name="limit">A limit for this wait alone, overriding the pipeline's.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<bool> Until(Func<CancellationToken, Task<bool>> condition, TimeSpan? limit = null, CancellationToken ct = default)
        {
            var context = ResilienceContextPool.Shared.Get(ct);
            if (limit is { } own)
            {
                context.Properties.Set(Limit, own);
            }

            try
            {
                return await pipeline.ExecuteAsync(async inner => await condition(inner.CancellationToken), context);
            }
            catch (TimeoutRejectedException)
            {
                return false;
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }
    }
}
