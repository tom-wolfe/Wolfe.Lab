using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Workflows.CaddyRoutes.Models;

namespace Wolfe.Lab.Build.Workflows.CaddyRoutes.Steps;

/// <summary>
/// Puts the gathered routes where the Caddyfile imports them from.
/// </summary>
/// <remarks>
/// A release like any other, so a route that was removed from its component is removed from the
/// door, and a rehearsal shows what would change.
/// </remarks>
[Step("install routes", StepKind.Work)]
internal sealed class InstallRoutes(IReleaseInstaller installer, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(StagedRoutes staged, Release release, CancellationToken ct = default)
    {
        await installer.Install(staged.Directory, release.Directory, ct);
        if (!job.DryRun)
        {
            log.Status($"Installed {staged.Names.Count} route{(staged.Names.Count == 1 ? "" : "s")} into {release.Directory.AbsolutePath}.");
        }

        return StepResult.Successful;
    }
}
