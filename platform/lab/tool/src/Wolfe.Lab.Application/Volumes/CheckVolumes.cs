using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Application.Volumes;

/// <summary>
/// At boot, Docker restarts containers before macOS mounts the drives, and an unmounted
/// <c>/Volumes</c> path is just a directory on the internal disk.
/// </summary>
[Step("guard volumes", StepKind.Check)]
internal sealed class CheckVolumes(IWorkflowLog log)
{
    internal const string Sentinel = ".lab-volume";

    public StepResult Run(DeploymentUnit unit)
    {
        var volumes = unit.RequiresVolumes.Select(volume => volume.Directory).ToList();
        var missing = volumes.Where(v => !v.GetFile(Sentinel).Exists).ToList();
        if (missing.Count > 0)
        {
            return StepResult.Failed(missing.Select(v => new Error(
                $"No sentinel at {v.AbsolutePath}/{Sentinel}: the drive is unmounted, or the sentinel was never created. " +
                "Refusing to converge onto a shadow path.")));
        }

        log.Detail(volumes.Count == 0 ? "No external volumes to check." : "Every required volume is mounted.");
        return StepResult.Successful;
    }
}
