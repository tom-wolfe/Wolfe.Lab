using Wolfe.Lab.Build.Backup.Models;

namespace Wolfe.Lab.Build.Backup.Steps;

/// <summary>
/// These schemas migrate forward only, so state is restored onto the image that wrote it. A
/// snapshot from a newer image on an older one is the failure every runbook warns about, and
/// the other direction is a migration nobody asked for.
/// </summary>
[Step("check image", StepKind.Check)]
internal sealed class CheckImage(RestoreRequest request, IWorkflowLog log)
{
    public StepResult Run(RestorePoint point, SnapshotImage current)
    {
        if (point.Snapshot.Image is not { } taken || current.Tag is not { } running)
        {
            log.Detail("No image on one side or the other: nothing to hold the restore to.");
            return StepResult.Successful;
        }

        if (taken == running)
        {
            log.Detail($"The stack runs {running}, the image the snapshot was taken under.");
            return StepResult.Successful;
        }

        if (request.AnyImage)
        {
            log.Warning($"Restoring state written under {taken} onto {running}, as asked.");
            return StepResult.Successful;
        }

        return new Error(
            $"The snapshot was taken under {taken} and the stack runs {running}. " +
            "Pin compose.yaml to the snapshot's image and converge first, or pass --any-image.");
    }
}
