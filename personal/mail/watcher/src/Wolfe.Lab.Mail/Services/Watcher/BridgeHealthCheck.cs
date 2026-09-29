using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Wolfe.Lab.Mail.Services.Watcher;

/// <summary>
/// Reports whether Bridge is answering on the lab network.
/// </summary>
/// <param name="options">Where Bridge answers.</param>
internal sealed class BridgeHealthCheck(IOptions<MailboxWatcherOptions> options) : IHealthCheck
{
    /// <summary>A probe is a question about reachability, not patience.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var where = $"{options.Value.BridgeHost}:{options.Value.ImapPort}";
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Timeout);

        try
        {
            using var socket = new TcpClient();
            await socket.ConnectAsync(options.Value.BridgeHost, options.Value.ImapPort, deadline.Token);
            return HealthCheckResult.Healthy($"{where} answers.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"{where} did not answer within {Timeout.TotalSeconds:0}s.");
        }
        catch (SocketException failure)
        {
            return HealthCheckResult.Unhealthy($"{where} — {failure.Message}");
        }
    }
}
