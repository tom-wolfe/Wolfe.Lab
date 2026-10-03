using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Telemetry;
using Wolfe.Lab.Application.Workflows.Agents.Models;
using Wolfe.Lab.Application.Workflows.Agents.Steps;

namespace Wolfe.Lab.Application.Workflows.Agents.Jobs;

/// <summary>
/// Proves the component's declarations are sound before any node converges on them.
/// </summary>
internal sealed class CheckJob : LabJob<AgentsOptions>
{
    public override string Name => "check";

    public override string Description => "Checks every node's agent declarations.";

    public override IReadOnlyList<Step> Steps { get; } = [
        Step.FromType<GatePathFilter>(),
        Step.FromType<CheckServiceCatalog>(),
        Step.FromType<CheckAgentDeclarations>(),
        Step.FromType<CheckTelemetryNames>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<AgentsOptions> options) => options
        .Require(s => s.Nodes.Count > 0, "'nodes' names no node in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, AgentsOptions options)
    {
        base.Configure(builder, options);
        builder.Services.AddSingleton(new AgentsDeclaredPerNode(options.Nodes));
    }
}
