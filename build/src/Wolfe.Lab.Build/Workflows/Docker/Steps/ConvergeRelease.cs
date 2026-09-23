using Ritten.Docker;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Workflows.Docker.Models;

namespace Wolfe.Lab.Build.Workflows.Docker.Steps;

/// <summary>
/// Converges the stack from the release, the resolved secrets in its environment.
/// </summary>
/// <remarks>
/// Ritten's own <c>ComposeUp</c> converges the component in the checkout; this one is the
/// lab's variant, because here the node runs the installed copy and the compose file reads
/// its secrets from the environment of this one invocation.
/// </remarks>
[Step("converge release", StepKind.Publish)]
internal sealed class ConvergeRelease(IDocker docker, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(Release release, ComposeEnvironment environment, CancellationToken ct = default)
    {
        await docker.ComposeUp(release.Directory, environment.Variables, ct);
        if (!job.DryRun)
        {
            log.Status($"Converged {release.Name}.");
        }

        return StepResult.Successful;
    }
}
