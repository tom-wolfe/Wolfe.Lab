using Microsoft.Extensions.Hosting;

namespace Wolfe.Lab.Mail.Services.Watcher;

/// <summary>
/// Runs the mailbox watcher.
/// </summary>
internal sealed class MailboxWatcherHost(MailboxWatcher watcher) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => watcher.Run(stoppingToken);
}
