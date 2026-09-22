using Ritten.Docker;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Deploy.Steps;

/// <summary>
/// Converges the stack from the release.
/// </summary>
[Step("compose up", StepKind.Publish)]
internal sealed class ComposeUp(IDocker docker, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(Slice slice, ComposeEnvironment environment, CancellationToken ct = default)
    {
        await docker.ComposeUp(slice.Release, environment.Variables, ct);
        if (!job.DryRun)
        {
            // The rehearsal has already said what it would do; saying it again here would be
            // the job claiming it happened.
            log.Status($"Converged {slice.Name}.");
        }

        return StepResult.Successful;
    }
}
