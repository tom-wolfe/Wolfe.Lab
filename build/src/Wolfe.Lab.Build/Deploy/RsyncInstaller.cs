namespace Wolfe.Lab.Build.Deploy;

/// <summary>
/// rsync, so a running container never sees its directory vanish the way a delete-and-copy
/// would make it.
/// </summary>
internal sealed class RsyncInstaller(ICommandRunner commands) : ISliceInstaller
{
    /// <inheritdoc />
    public async Task Install(IDirectory source, IDirectory release, CancellationToken ct = default)
    {
        release.Create();
        await commands.Run(Rsync(source, release).ThrowOnError(), ct);
    }

    /// <summary>
    /// The trailing slash on the source copies its contents rather than the directory itself.
    /// </summary>
    internal static Command Rsync(IDirectory source, IDirectory release, bool dryRun = false)
    {
        var command = Command.Create("rsync")
            .WithArguments("-a", "--delete", "--exclude", "tofu/", "--exclude", "ritten.json");
        if (dryRun)
        {
            command = command.AndArguments("--dry-run", "--itemize-changes");
        }

        return command.AndArguments($"{source.AbsolutePath}/", $"{release.AbsolutePath}/");
    }
}
