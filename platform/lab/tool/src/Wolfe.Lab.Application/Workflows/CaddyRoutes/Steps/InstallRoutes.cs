using Wolfe.Lab.Application.Workflows.CaddyRoutes.Models;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.CaddyRoutes.Steps;

/// <summary>
/// Puts the gathered routes where the Caddyfile imports them from.
/// </summary>
/// <remarks>
/// Installed like any deployment, so a route that was removed from its component is removed from the
/// door, and a rehearsal shows what would change.
/// </remarks>
[Step("install routes", StepKind.Work)]
internal sealed class InstallRoutes(IReleaseInstaller installer, IOptions<LabDirectories> options, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(StagedRoutes staged, DeploymentUnit unit, CancellationToken ct = default)
    {
        var installed = options.Value.DeployedTo(unit);
        await installer.Install(staged.Directory, installed, ct);
        if (!job.DryRun)
        {
            log.Status($"Installed {staged.Names.Count} route{(staged.Names.Count == 1 ? "" : "s")} into {installed.AbsolutePath}.");
        }

        return StepResult.Successful;
    }
}
