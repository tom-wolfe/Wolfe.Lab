using Ritten.Docker;

namespace Wolfe.Lab.Build.Backup.Steps;

/// <summary>
/// Reads the image the service runs, before anything is stopped, so the snapshot carries the
/// version that wrote it.
/// </summary>
[Step("resolve image", StepKind.Work)]
internal sealed class ResolveImage(IDocker docker, BackupPlan plan, IWorkflowLog log)
{
    public async Task<StepResult<SnapshotImage>> Run(CancellationToken ct = default)
    {
        if (plan.Image is null)
        {
            log.Detail("No container named: nothing to pair a restore with.");
            return SnapshotImage.None;
        }

        var state = await docker.Inspect(plan.Image, ct);
        log.Detail($"{plan.Image} runs {state.Image}.");
        return new SnapshotImage(state.Image);
    }
}
