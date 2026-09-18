using Wolfe.Lab.Build.Restic.Models;

namespace Wolfe.Lab.Build.Restic.Steps;

/// <summary>
/// The one place retention lives: the same policy on both repositories, after the copy.
/// Snapshots group by host and path, so each service thins out on its own.
/// </summary>
[Step("apply retention", StepKind.Work)]
internal sealed class ApplyRetention(IRestic restic, RetentionPolicy policy, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(ResticRepository local, OffsiteRepository offsite, CancellationToken ct = default)
    {
        await restic.Prune(local, policy, ct);
        await restic.Prune(offsite.Repository, policy, ct);
        if (!job.DryRun)
        {
            log.Detail($"Retention applied to {local.Location} and {offsite.Repository.Location}.");
        }

        return StepResult.Successful;
    }
}
