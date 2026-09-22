namespace Wolfe.Lab.Build.Clients.Obsidian;

/// <summary>
/// Runs <c>ob</c>, the npm global under nvm, through the <c>nvm-run</c> wrapper chezmoi places in
/// <c>~/.local/bin</c>.
/// </summary>
internal sealed class ObsidianClient(ICommandRunner commands) : IObsidian
{
    private static string NvmRun =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "nvm-run");

    /// <inheritdoc />
    public async Task Sync(IDirectory vault, CancellationToken cancellationToken = default) =>
        await commands.Run(
            Command.Create(NvmRun).WithArguments("ob", "sync", "--path", vault.AbsolutePath).ThrowOnError(),
            cancellationToken);
}
