using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Workflows.Restic.Models;

namespace Wolfe.Lab.Build.Workflows.Restic.Steps;

/// <summary>
/// Ships every snapshot the offsite repository does not have. Before retention, so nothing is
/// pruned before it is offsite.
/// </summary>
[Step("copy offsite", StepKind.Work)]
internal sealed class CopyOffsite(IRestic restic, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(OffsiteRepository offsite, CancellationToken ct = default)
    {
        await restic.Copy(offsite, ct);
        if (!job.DryRun)
        {
            log.Status($"Copied new snapshots to {offsite.Repository.Location}.");
        }

        return StepResult.Successful;
    }
}
