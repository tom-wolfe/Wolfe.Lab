using Ritten.Docker;
using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Telemetry;

namespace Wolfe.Lab.Application.Workflows.Agents.Jobs;

/// <summary>
/// Proves the component's declarations are sound before any node converges on them.
/// </summary>
internal sealed class CheckJob : LabJob<DeclaredSettings>
{
    public override string Name => "check";

    public override string Description => "Checks every node's agent declarations.";

    public override IReadOnlyList<Step> Steps { get; } = [
        Step.FromType<GatePathFilter>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<CheckAgentDeclarations>(),
        Step.FromType<CheckTelemetryNames>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddDocker();
    }
}
