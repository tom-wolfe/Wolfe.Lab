using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Facets.Heartbeats;
using Wolfe.Lab.Infrastructure.Heartbeat;

namespace Wolfe.Lab.Application.Heartbeat;

/// <summary>
/// Finds the check the deployment pings when its work is done: the one its components declare, or
/// its <c>ritten.json</c>'s until they do.
/// </summary>
[Step("resolve heartbeat", StepKind.Work)]
internal sealed class ResolveHeartbeat(HeartbeatCheckOptions former, IWorkflowLog log)
{
    public StepResult<HeartbeatCheck> Run(DeploymentUnit unit)
    {
        var checks = unit.Components.Select(component => component.Heartbeat).OfType<HeartbeatCheck>().ToList();
        if (checks.Count == 0 && former.ToCheck() is { } written)
        {
            checks.Add(written);
        }

        if (checks is not [var check])
        {
            return new Error($"{unit} declares {checks.Count} heartbeats: a job pings the one its deployment declares.");
        }

        log.Detail($"Pings {check.Slug} when done.");
        return check;
    }
}
