using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Wolfe.Lab.Mail.Services.Watcher;

/// <summary>
/// Reports whether the mailbox session is still turning over.
/// </summary>
/// <param name="state">What the session last managed.</param>
internal sealed class MailboxHealthCheck(MailboxState state) : IHealthCheck
{
    /// <summary>
    /// The idle loop is capped at 9 minutes, so this tolerates one slow pass and nothing more.
    /// </summary>
    private static readonly TimeSpan Stale = TimeSpan.FromMinutes(15);

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = state.Since switch
        {
            null => Unhealthy("Never reached the mailbox."),
            { } since when since > Stale => Unhealthy($@"No mailbox activity for {since:hh\:mm\:ss}."),
            { } since => HealthCheckResult.Healthy($@"Watching; last pass {since:hh\:mm\:ss} ago.")
        };
        return Task.FromResult(result);
    }

    /// <summary>
    /// Carries the exception itself, so the failed check's log has the stack trace, and links the
    /// probe's span to the one the session ended in, which is where the rest of the story is.
    /// </summary>
    private HealthCheckResult Unhealthy(string what)
    {
        if (state.Failure is not { } failure)
        {
            return HealthCheckResult.Unhealthy(what);
        }

        var reason = $"{what} {failure.GetType().Name}: {failure.Message}";
        if (state.FailedIn == default)
        {
            return HealthCheckResult.Unhealthy(reason, failure);
        }

        Activity.Current?.AddLink(new ActivityLink(state.FailedIn));
        return HealthCheckResult.Unhealthy($"{reason} (trace {state.FailedIn.TraceId})", failure);
    }
}
