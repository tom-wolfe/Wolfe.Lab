using Ritten.Git;
using Wolfe.Lab.Domain.Catalog.Components.Obsidian;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Steps;

/// <summary>
/// Records what the sync pass changed as one commit.
/// </summary>
[Step("commit vault", StepKind.Work)]
internal sealed class CommitVault(IGit git, IWorkflowLog log)
{
    internal const string Message = "Sync from Obsidian";

    public async Task<StepResult> Run(ObsidianComponent vault, CancellationToken cancellationToken = default)
    {
        var repository = git.InRepository(vault.Path.Directory);
        if (!await repository.IsRepository(cancellationToken))
        {
            return new Error($"{vault.Path.Directory.AbsolutePath} is not a git repository — see personal/obsidian/RUNBOOK.md.");
        }

        // Written before anything is counted, so an excluded path never shows up as a change.
        await WriteExcludes(vault, cancellationToken);

        switch (await repository.GetRemoteUrl("origin", cancellationToken))
        {
            case null:
                await repository.AddRemote("origin", vault.Repository.Value, cancellationToken);
                break;
            case var url when url != vault.Repository.Value:
                // A push to the wrong place creates a repository there (Forgejo's push-to-create),
                // so a checkout whose remote drifted from its declaration stops here, with the fix.
                return new Error(
                    $"{vault.Path.Directory.AbsolutePath} pushes to {url}, but its declaration says {vault.Repository.Value}. " +
                    $"Re-point it: git -C {vault.Path.Directory.AbsolutePath} remote set-url origin {vault.Repository.Value}");
        }

        var changed = await repository.ChangedFiles(".", cancellationToken);
        if (changed.Count == 0)
        {
            log.Detail("Nothing changed since the last commit.");
            return StepResult.Successful;
        }

        await repository.Stage(".", cancellationToken);
        await repository.Commit(Message, cancellationToken);
        log.Status($"Committed {changed.Count} changed path{(changed.Count == 1 ? "" : "s")}.");
        return StepResult.Successful;
    }

    /// <summary>
    /// <c>.git/info/exclude</c> rather than a <c>.gitignore</c>: the list is the lab's, and a
    /// file inside the vault would sync to every device.
    /// </summary>
    private static Task WriteExcludes(ObsidianComponent vault, CancellationToken cancellationToken)
    {
        var lines = vault.Exclude.Prepend("# Written by `lab sync` from the vault's declaration; edits here are overwritten.");
        return vault.Path.Directory.GetDirectory(".git").GetDirectory("info").GetFile("exclude").WriteAllText(string.Join('\n', lines) + '\n', cancellationToken: cancellationToken);
    }
}
