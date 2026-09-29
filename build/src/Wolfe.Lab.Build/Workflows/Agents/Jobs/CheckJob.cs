using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.Agents.Models;
using Wolfe.Lab.Build.Workflows.Agents.Steps;

namespace Wolfe.Lab.Build.Workflows.Agents.Jobs;

/// <summary>
/// Proves the component's declarations are sound before any node converges on them.
/// </summary>
internal sealed class CheckJob : LabJob<AgentsOptions>
{
    public override string Name => "check";

    public override string Description => "Checks every node's agent declarations.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<GatePathFilter>(), Step.FromType<CheckAgentDeclarations>()];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<AgentsOptions> options) => options
        .Require(s => s.Nodes.Count > 0, "'nodes' names no node in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, AgentsOptions options)
    {
        base.Configure(builder, options);
        builder.Services.AddSingleton(new AgentsDeclaredPerNode(options.Nodes));
    }
}
