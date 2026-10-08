using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Chezmoi;

namespace Wolfe.Lab.Application.Workflows.Chezmoi.Steps;

/// <summary>
/// The profiles the component declares.
/// </summary>
[Step("resolve profiles", StepKind.Work)]
internal sealed class ResolveProfiles(IWorkflowLog log)
{
    public StepResult<ChezmoiComponent> Run(DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(WorkflowName.Chezmoi).TryGetValue(out var declared, out var errors))
        {
            return StepResult.Failed(errors);
        }

        var profiles = (ChezmoiComponent)declared;
        log.Detail($"Renders {string.Join(", ", profiles.Profiles.Select(profile => profile.Value))}.");
        return profiles;
    }
}
