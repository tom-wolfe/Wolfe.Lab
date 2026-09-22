using Ritten.Git;

namespace Wolfe.Lab.Build.Workflows.Common.Steps;

/// <summary>
/// Ends the job, successfully, when the pull request did not touch this component.
/// </summary>
[Step("path filter gate", StepKind.Gate)]
internal sealed class GatePathFilter(PullRequest pullRequest, IGit git, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (pullRequest.BaseRef is not { Length: > 0 } baseRef)
        {
            // Not reviewing anything: a deploy is scoped by its trigger, and a local run was
            // asked for on purpose.
            log.Detail("Not a pull request; checking everything.");
            return StepResult.Successful;
        }

        // The remote-tracking ref: a checkout fetches the base branch without checking it out,
        // so the local name usually does not exist. Resolving it is the point at which a shallow
        // checkout fails loudly rather than reporting nothing changed.
        var changed = await git.ChangedFilesSince($"origin/{baseRef}", ".", cancellationToken);
        if (changed.Count > 0)
        {
            log.Detail($"{changed.Count} file(s) changed here since {baseRef}.");
            return StepResult.Successful;
        }

        log.Status($"Nothing here changed since {baseRef}.");
        return StepResult.NothingToDo;
    }
}
