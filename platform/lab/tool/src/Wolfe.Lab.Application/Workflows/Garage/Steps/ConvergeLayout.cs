using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Garage;
using Wolfe.Lab.Infrastructure.Garage;

namespace Wolfe.Lab.Application.Workflows.Garage.Steps;

/// <summary>
/// Gives the node the role its component declares: on a new node its first, after a change to
/// the declaration the next version, and otherwise nothing.
/// </summary>
[Step("converge layout", StepKind.Publish)]
internal sealed class ConvergeLayout(IGarage garage, IWorkflowLog log)
{
    public async Task<StepResult> Run(DeploymentUnit unit, CancellationToken ct = default)
    {
        if (!unit.ByWorkflow(WorkflowName.Garage).TryGetValue(out var component, out var errors))
        {
            return StepResult.Failed(errors);
        }

        var declared = ((GarageComponent)component).Layout;
        var node = await garage.NodeId(ct);
        var current = await garage.Layout(ct);
        if (current.RoleOf(node) == declared)
        {
            log.Status($"The layout already gives the node its role (version {current.Version}).");
            return StepResult.Successful;
        }

        var version = current.Version + 1;
        log.Status($"Applying layout version {version}: node {node} in zone {declared.Zone.Value}, storing {declared.Capacity.Value} bytes.");
        await garage.AssignLayout(node, declared, ct);
        await garage.ApplyLayout(version, ct);
        return StepResult.Successful;
    }
}
