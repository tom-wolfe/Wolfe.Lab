using Ritten.Docker;

namespace Wolfe.Lab.Build.Docker.Steps;

/// <summary>
/// Proves compose can read the component's file before anything is built or deployed from it.
/// </summary>
/// <remarks>
/// Reads the checkout, not the release: a check runs before the merge, when nothing is
/// installed. Secrets are absent there and that is fine — they reach compose through its
/// environment, and an interpolation with nothing behind it resolves empty rather than failing.
/// </remarks>
[Step("compose check", StepKind.Check)]
internal sealed class ComposeCheck(IDocker docker, IFileSystem fileSystem, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (await docker.ComposeValidate(fileSystem.ProjectRoot, ct: cancellationToken) is { } error)
        {
            return new Error(error);
        }

        log.Status("The compose file is valid.");
        return StepResult.Successful;
    }
}
