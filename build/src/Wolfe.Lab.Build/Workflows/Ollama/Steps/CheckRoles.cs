using Wolfe.Lab.Build.Workflows.Ollama.Models;

namespace Wolfe.Lab.Build.Workflows.Ollama.Steps;

/// <summary>
/// Judges the component's roles on its own file, and against every other server of the slice.
/// </summary>
/// <remarks>
/// Either side of a disagreement fails its own check, so a pull request that changes only one
/// component's file is still caught by that component.
/// </remarks>
[Step("check roles", StepKind.Check)]
internal sealed class CheckRoles(DeclaredRoles declared, IFileSystem fileSystem, IWorkflowLog log)
{
    public StepResult Run()
    {
        var siblings = SliceComponents.Read(fileSystem.ProjectRoot);
        var errors = RoleRules.Local(declared.Models)
            .Concat(RoleRules.Across(siblings))
            .Select(message => new Error(message))
            .ToList();

        if (errors.Count > 0)
        {
            return StepResult.Failed(errors);
        }

        log.Detail(declared.Models.Roles.Count == 0
            ? "No roles declared."
            : $"{declared.Models.Roles.Count} role(s), agreeing with {siblings.Count - 1} other server(s).");
        return StepResult.Successful;
    }
}
