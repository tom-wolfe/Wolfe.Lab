using Wolfe.Lab.Build.Clients.Chezmoi;

namespace Wolfe.Lab.Build.Workflows.Chezmoi.Steps;

/// <summary>
/// Makes this node match the source: <c>chezmoi update</c>, which pulls the node's own clone and
/// applies it.
/// </summary>
[Step("update node", StepKind.Publish)]
internal sealed class UpdateNode(IChezmoi chezmoi, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        await chezmoi.Update(ct);
        if (!job.DryRun)
        {
            log.Status("Updated this node.");
        }

        return StepResult.Successful;
    }
}
