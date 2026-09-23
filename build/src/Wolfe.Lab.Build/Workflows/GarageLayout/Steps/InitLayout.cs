using Wolfe.Lab.Build.Clients.Garage;
using Wolfe.Lab.Build.Workflows.GarageLayout.Models;

namespace Wolfe.Lab.Build.Workflows.GarageLayout.Steps;

/// <summary>
/// Applies the first layout, once: a no-op as soon as any version exists.
/// </summary>
[Step("init layout", StepKind.Publish)]
internal sealed class InitLayout(IGarage garage, Layout layout, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        var version = await garage.LayoutVersion(ct);
        if (version > 0)
        {
            log.Status($"Layout already applied (version {version}).");
            return StepResult.Successful;
        }

        var node = await garage.NodeId(ct);
        log.Status($"Applying the initial layout: node {node}, zone {layout.Zone}, capacity {layout.Capacity}.");
        await garage.AssignLayout(node, layout.Zone, layout.Capacity, ct);
        await garage.ApplyLayout(1, ct);
        return StepResult.Successful;
    }
}
