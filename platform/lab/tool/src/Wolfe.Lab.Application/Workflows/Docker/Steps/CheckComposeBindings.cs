using Ritten.Docker;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Infrastructure.Compose;

namespace Wolfe.Lab.Application.Workflows.Docker.Steps;

/// <summary>
/// Validates a the compose project matches up with what the component metadata declares.
/// </summary>
[Step("check compose bindings", StepKind.Check)]
internal sealed class CheckComposeBindings(IDocker docker, IFileSystem fileSystem, IWorkflowLog log)
{
    public async Task<StepResult> Run(DeploymentUnit unit, CancellationToken ct = default)
    {
        if (!(await docker.ComposeConfig(fileSystem.ProjectRoot, ct: ct)).TryGetValue(out var project, out var unreadable))
        {
            return StepResult.Failed(ComposeErrors.Unreadable(fileSystem.ProjectRoot, unreadable));
        }

        if (!ComposeBindings.Of(unit, project).TryGetValue(out var bindings, out var errors))
        {
            return StepResult.Failed(errors);
        }

        log.Detail($"Each of the stack's {bindings.Bindings.Count} services is one component's: {Bound(bindings)}.");
        return StepResult.Successful;
    }

    private static string Bound(ComposeBindings bindings) =>
        string.Join(", ", bindings.Bindings.Select(binding => $"{binding.Service.Name} as {binding.Component.Name}"));
}
