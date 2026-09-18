using Ritten.Docker;
using Wolfe.Lab.Build.Backup.Models;

namespace Wolfe.Lab.Build.Backup.Steps;

/// <summary>
/// Reads the image the service runs, before it is stopped, so the snapshot carries the version
/// that wrote it.
/// </summary>
[Step("resolve image", StepKind.Work)]
internal sealed class ResolveImage(IDocker docker, BackupPlan plan, IWorkflowLog log)
{
    public async Task<StepResult<SnapshotImage>> Run(CancellationToken ct = default)
    {
        if (plan.Container is null)
        {
            log.Detail("A warm snapshot: no container to stop, no image to pair a restore with.");
            return SnapshotImage.None;
        }

        var state = await docker.Inspect(plan.Container, ct);
        log.Detail($"{plan.Container} runs {state.Image}.");
        return new SnapshotImage(state.Image);
    }
}
