using System.Globalization;
using Ritten.Docker;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Application.Workflows.Docker.Models;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.Docker.Steps;

/// <summary>
/// Converges the stack from where it is installed, the resolved secrets in its environment.
/// </summary>
/// <remarks>
/// Ritten's own <c>ComposeUp</c> converges the component in the checkout; this one is the
/// lab's variant, because here the node runs the installed copy and the compose file reads
/// its secrets from the environment of this one invocation.
/// </remarks>
[Step("converge stack", StepKind.Publish)]
internal sealed class ConvergeStack(IDocker docker, IOptions<LabDirectories> options, IWorkflowReport report, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(DeploymentUnit unit, ComposeEnvironment composeEnvironment, PublishedArtifacts artifacts, CancellationToken ct = default)
    {
        await docker.ComposeUp(options.Value.DeployedTo(unit), composeEnvironment.Variables, ct);
        await RestartForChangedFiles(unit, artifacts, ct);
        if (!job.DryRun)
        {
            log.Status($"Converged {unit.Name}.");
        }

        return StepResult.Successful;
    }

    /// <summary>
    /// Restarts the stack when its files changed since it was last started for them.
    /// </summary>
    /// <remarks>
    /// <c>up</c> recreates a container whose definition changed, never one whose bind-mounted
    /// files did: a service reading its config at start would go on running the old one. The
    /// stamp it was last restarted for is kept beside the installed stacks, and written only once
    /// the restart has happened — so a restart that failed is still owed on the next deploy. A
    /// stack with no stamp yet is taken as current: its containers were just created from it.
    /// </remarks>
    private async Task RestartForChangedFiles(DeploymentUnit unit, PublishedArtifacts artifacts, CancellationToken ct)
    {
        if (artifacts.Stamp is not { } stamp)
        {
            return;
        }

        var roots = options.Value;
        var file = roots.AppliedStamp(unit);
        var current = stamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        var applied = (await file.ReadAllTextIfExists(ct))?.Trim();
        if (applied == current)
        {
            return;
        }

        if (job.DryRun)
        {
            log.Skipped(applied is null
                ? $"Would record {unit.Name}'s files as current."
                : $"Would restart {unit.Name}: its files changed at {current}, after the last restart for them.");
            if (applied is not null)
            {
                report.Section(ReportSections.Artifacts).Note($"Would restart `{unit.Name}` for its changed files.");
            }

            return;
        }

        if (applied is not null)
        {
            await docker.ComposeStop(roots.DeployedTo(unit), ct);
            await docker.ComposeStart(roots.DeployedTo(unit), ct);
            log.Status($"Restarted {unit.Name} for its changed files.");
            report.Section(ReportSections.Artifacts).Success($"Restarted `{unit.Name}` for its changed files.");
        }

        await file.WriteAllText(current, cancellationToken: ct);
    }
}
