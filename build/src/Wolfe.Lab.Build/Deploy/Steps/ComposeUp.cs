using Ritten.Docker;
using Wolfe.Lab.Build.Deploy.Models;

namespace Wolfe.Lab.Build.Deploy.Steps;

/// <summary>
/// Converges the stack from the release.
/// </summary>
[Step("compose up", StepKind.Publish)]
internal sealed class ComposeUp(IDocker docker, IWorkflowLog log)
{
    public async Task<StepResult> Run(Slice slice, ComposeEnvironment environment, CancellationToken ct = default)
    {
        await docker.ComposeUp(slice.Release, environment.Variables, ct);
        log.Status($"Converged {slice.Name}.");
        return StepResult.Successful;
    }
}
