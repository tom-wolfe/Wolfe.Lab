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
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.Since switch
        {
            null => HealthCheckResult.Unhealthy($"Never reached the mailbox. {state.Failure}".TrimEnd()),
            { } since when since > Stale => HealthCheckResult.Unhealthy(
                $"No mailbox activity for {since:hh\\:mm\\:ss}. {state.Failure}".TrimEnd()),
            { } since => HealthCheckResult.Healthy($"Watching; last pass {since:hh\\:mm\\:ss} ago.")
        });
}
