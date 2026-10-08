using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Obsidian;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Steps;

/// <summary>
/// The vault the component is, checked out where it says.
/// </summary>
[Step("resolve vault", StepKind.Check)]
internal sealed class ResolveVault
{
    public StepResult<ObsidianComponent> Run(DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(WorkflowName.Obsidian).TryGetValue(out var declared, out var errors))
        {
            return StepResult.Failed(errors);
        }

        var vault = (ObsidianComponent)declared;
        return vault.Path.Directory.Exists
            ? vault
            : new Error($"{vault.Path.Directory.AbsolutePath} does not exist — see personal/obsidian/RUNBOOK.md.");
    }
}
