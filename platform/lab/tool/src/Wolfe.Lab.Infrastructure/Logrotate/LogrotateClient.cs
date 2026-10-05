namespace Wolfe.Lab.Infrastructure.Logrotate;

/// <summary>
/// Runs the logrotate the node has: chezmoi installs it on a Mac (the Brewfile), and the Pi's OS
/// ships it, in /usr/sbin, which its runner has on its path.
/// </summary>
internal sealed class LogrotateClient(ICommandRunner commands) : ILogrotate
{
    /// <inheritdoc />
    public async Task Rotate(IFile configuration, IFile state, CancellationToken ct = default) =>
        await commands.Run(Command.Create("logrotate").WithArguments("--state", state.AbsolutePath, configuration.AbsolutePath).ThrowOnError(), ct);
}
