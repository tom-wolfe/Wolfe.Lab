using System.Globalization;
using Ritten.Docker;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Workflows.Docker.Models;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.Docker.Steps;

/// <summary>
/// Converges the stack from the release, the resolved secrets in its environment.
/// </summary>
/// <remarks>
/// Ritten's own <c>ComposeUp</c> converges the component in the checkout; this one is the
/// lab's variant, because here the node runs the installed copy and the compose file reads
/// its secrets from the environment of this one invocation.
/// </remarks>
[Step("converge release", StepKind.Publish)]
internal sealed class ConvergeRelease(IDocker docker, WorkflowEnvironment environment, IWorkflowReport report, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(Release release, ComposeEnvironment composeEnvironment, PublishedArtifacts artifacts, CancellationToken ct = default)
    {
        await docker.ComposeUp(release.Directory, composeEnvironment.Variables, ct);
        await RestartForChangedFiles(release, artifacts, ct);
        if (!job.DryRun)
        {
            log.Status($"Converged {release.Name}.");
        }

        return StepResult.Successful;
    }

    /// <summary>
    /// Restarts the stack when its files changed since it was last started for them.
    /// </summary>
    /// <remarks>
    /// <c>up</c> recreates a container whose definition changed, never one whose bind-mounted
    /// files did: a service reading its config at start would go on running the old one. The
    /// stamp it was last restarted for is kept beside the releases, and written only once the
    /// restart has happened — so a restart that failed is still owed on the next deploy. A
    /// release with no stamp yet is taken as current: its containers were just created from it.
    /// </remarks>
    private async Task RestartForChangedFiles(Release release, PublishedArtifacts artifacts, CancellationToken ct)
    {
        if (artifacts.Stamp is not { } stamp)
        {
            return;
        }

        var roots = LabRoots.From(environment);
        var file = roots.AppliedStamp(release.Name);
        var current = stamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        var applied = File.Exists(file) ? (await File.ReadAllTextAsync(file, ct)).Trim() : null;
        if (applied == current)
        {
            return;
        }

        if (job.DryRun)
        {
            log.Skipped(applied is null
                ? $"Would record {release.Name}'s files as current."
                : $"Would restart {release.Name}: its files changed at {current}, after the last restart for them.");
            if (applied is not null)
            {
                report.Section(PublishArtifacts.Section).Note($"Would restart `{release.Name}` for its changed files.");
            }

            return;
        }

        if (applied is not null)
        {
            await docker.ComposeStop(release.Directory, ct);
            await docker.ComposeStart(release.Directory, ct);
            log.Status($"Restarted {release.Name} for its changed files.");
            report.Section(PublishArtifacts.Section).Success($"Restarted `{release.Name}` for its changed files.");
        }

        Directory.CreateDirectory(roots.Applied);
        await File.WriteAllTextAsync(file, current, ct);
    }
}
