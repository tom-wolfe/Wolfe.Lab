using Ritten.Git;
using Wolfe.Lab.Application.Workflows.Obsidian.Models;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Steps;

/// <summary>
/// Pushes what the checkout has that Forgejo doesn't.
/// </summary>
[Step("push vault", StepKind.Publish)]
internal sealed class PushVault(IGit git, ISecretProvider secrets, IWorkflowLog log)
{
    public async Task<StepResult> Run(Vault vault, CancellationToken ct = default)
    {
        var repository = git.InRepository(vault.Directory);
        if (await repository.CurrentBranch(ct) is not { } branch)
        {
            return new Error($"{vault.Directory.AbsolutePath} has no branch checked out.");
        }

        var upstream = await repository.Upstream(ct);
        if (upstream is not null && await repository.CommitsAhead(upstream, ct) == 0)
        {
            log.Detail("Nothing to push.");
            return StepResult.Successful;
        }

        var token = await secrets.Resolve(vault.Push.Token.Value, ct);
        await repository.Push(
            "origin",
            branch,
            new GitCredential(vault.Push.Username.Value, token),
            setUpstream: upstream is null,
            ct
        );
        log.Status($"Pushed {branch} to {vault.Repository}.");
        return StepResult.Successful;
    }
}
