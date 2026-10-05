namespace Wolfe.Lab.Infrastructure.Logrotate;

/// <summary>
/// The rehearsal: logrotate's own debug run, which says what it would rotate and changes nothing,
/// its state file included.
/// </summary>
internal sealed class DryRunLogrotate(ICommandRunner commands) : ILogrotate
{
    /// <inheritdoc />
    public async Task Rotate(IFile configuration, IFile state, CancellationToken ct = default) =>
        await commands.Run(Command.Create("logrotate").WithArguments("--debug", "--state", state.AbsolutePath, configuration.AbsolutePath).ThrowOnError(), ct);
}
