using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Restic;

namespace Wolfe.Lab.Application.Workflows.Restic.Steps;

/// <summary>
/// Finds the repositories' declaration: what a prune keeps of them, and how much of the offsite
/// copy a check reads back.
/// </summary>
[Step("resolve repositories", StepKind.Work)]
internal sealed class ResolveRepositories(IWorkflowLog log)
{
    public StepResult<ResticComponent> Run(DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(WorkflowName.Restic).TryGetValue(out var declared, out var errors))
        {
            return StepResult.Failed(errors);
        }

        var repositories = (ResticComponent)declared;
        var retention = repositories.Retention;
        log.Detail($"Keeps {retention.Daily} daily, {retention.Weekly} weekly and {retention.Monthly} monthly; reads {repositories.VerifySample.Value}% of the offsite copy back.");
        return repositories;
    }
}
