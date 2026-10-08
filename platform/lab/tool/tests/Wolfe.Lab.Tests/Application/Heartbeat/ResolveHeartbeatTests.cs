using Wolfe.Lab.Application.Heartbeat;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Facets.Heartbeats;
using Wolfe.Lab.Domain.Secrets;
using Wolfe.Lab.Infrastructure.Heartbeat;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Heartbeat;

public class ResolveHeartbeatTests
{
    private static readonly SecretReference Key = SecretReference.From("op://Wolfe.Lab/healthchecks-ping-key/credential");

    // The ping, declaring a heartbeat or not.
    private static DeploymentUnit Unit(HeartbeatCheck? declared)
    {
        var unit = Catalogs.Unit("monitoring/heartbeat/ping", Catalogs.Definition("ping", ComponentKind.Backend, WorkflowName.Heartbeat));
        unit.Head.Heartbeat = declared;
        return unit;
    }

    private static StepResult<HeartbeatCheck> Resolve(HeartbeatCheck? declared, HeartbeatCheckOptions? former = null) =>
        new ResolveHeartbeat(former ?? new HeartbeatCheckOptions(), Substitute.For<IWorkflowLog>()).Run(Unit(declared));

    [Fact]
    public void Run_TakesTheCheckTheDeploymentDeclares()
    {
        var declared = new HeartbeatCheck(HeartbeatSlug.From("lab-chezmoi-update"), Key);

        Resolve(declared, new HeartbeatCheckOptions { Check = "lab-other", Key = Key }).Value.ShouldBe(declared);
    }

    [Fact]
    public void Run_TakesItsRittenJsonsUntilItDeclaresOne() =>
        Resolve(null, new HeartbeatCheckOptions { Check = "lab-chezmoi-update", Key = Key }).Value
            .ShouldBe(new HeartbeatCheck(HeartbeatSlug.From("lab-chezmoi-update"), Key));

    [Fact]
    public void Run_RefusesADeploymentWithNoneAnywhere() =>
        Resolve(null).Outcome.IsFailure.ShouldBeTrue();
}
