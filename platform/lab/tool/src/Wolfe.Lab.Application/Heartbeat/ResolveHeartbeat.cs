using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Facets.Heartbeats;

namespace Wolfe.Lab.Application.Heartbeat;

/// <summary>
/// Finds the check the deployment pings when its work is done: the one its components declare.
/// </summary>
[Step("resolve heartbeat", StepKind.Work)]
internal sealed class ResolveHeartbeat(IWorkflowLog log)
{
    public StepResult<HeartbeatCheck> Run(DeploymentUnit unit)
    {
        var checks = unit.Components.Select(component => component.Heartbeat).OfType<HeartbeatCheck>().ToList();
        if (checks is not [var check])
        {
            return new Error($"{unit} declares {checks.Count} heartbeats: a job pings the one its deployment declares.");
        }

        log.Detail($"Pings {check.Slug} when done.");
        return check;
    }
}
