using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Workflows.Agents.Models;
using Wolfe.Lab.Application.Workflows.Agents.Steps;
using Wolfe.Lab.Infrastructure.Logrotate;

namespace Wolfe.Lab.Application.Workflows.Agents.Jobs;

/// <summary>
/// Rotates this node's logs of the component's agent.
/// </summary>
internal sealed class RotateJob : LabJob<AgentsOptions>
{
    public override string Name => "rotate";

    public override string Description => "Rotates the logs the component's agent writes on this node.";

    public override JobKind Kind => JobKind.Work;

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveAgentDeclarations>(),
        Step.FromType<GateApproval>(),
        Step.FromType<RotateAgentLogs>()
    ];

    protected override void Configure(IWorkflowBuilder builder, AgentsOptions options)
    {
        base.Configure(builder, options);
        builder.AddLogrotate();
    }
}
