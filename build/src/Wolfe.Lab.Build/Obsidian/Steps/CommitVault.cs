using Ritten.Git;
using Wolfe.Lab.Build.Obsidian.Models;

namespace Wolfe.Lab.Build.Obsidian.Steps;

/// <summary>
/// Records what the sync pass changed as one commit.
/// </summary>
[Step("commit vault", StepKind.Work)]
internal sealed class CommitVault(IGit git, VaultExcludes excludes, IWorkflowLog log)
{
    internal const string Message = "Sync from Obsidian";

    public async Task<StepResult> Run(Vault vault, CancellationToken cancellationToken = default)
    {
        var repository = git.InRepository(vault.Directory);
        if (!await repository.IsRepository(cancellationToken))
        {
            return new Error($"{vault.Directory.AbsolutePath} is not a git repository — see obsidian/RUNBOOK.md.");
        }

        // Written before anything is counted, so an excluded path never shows up as a change.
        WriteExcludes(vault.Directory.GetDirectory(".git"));

        switch (await repository.GetRemoteUrl("origin", cancellationToken))
        {
            case null:
                await repository.AddRemote("origin", vault.Repository.Value, cancellationToken);
                break;
            case var url when url != vault.Repository.Value:
                // A push to the wrong place creates a repository there (Forgejo's push-to-create),
                // so a checkout whose remote drifted from ritten.json stops here, with the fix.
                return new Error(
                    $"{vault.Directory.AbsolutePath} pushes to {url}, but ritten.json says {vault.Repository.Value}. " +
                    $"Re-point it: git -C {vault.Directory.AbsolutePath} remote set-url origin {vault.Repository.Value}");
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
    private void WriteExcludes(IDirectory dotGit)
    {
        var info = dotGit.GetDirectory("info");
        info.Create();
        using var stream = info.GetFile("exclude").OpenWrite();
        stream.SetLength(0);
        using var writer = new StreamWriter(stream);
        writer.WriteLine("# Written by `lab sync` from obsidian/ritten.json; edits here are overwritten.");
        foreach (var pattern in excludes.Patterns)
        {
            writer.WriteLine(pattern);
        }
    }
}
