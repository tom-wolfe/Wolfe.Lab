using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Volumes;
using Wolfe.Lab.Application.Workflows.Agents.Models;
using Wolfe.Lab.Application.Workflows.Agents.Steps;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Packages;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Infrastructure.Volumes;

namespace Wolfe.Lab.Application.Workflows.Agents.Jobs;

/// <summary>
/// Converges one node's agents.
/// </summary>
internal sealed class DeployJob : LabJob<AgentsOptions>
{
    private static readonly JobArgument<string> Node =
        JobArgument.Value<string>("node",
            "The runner this runs on — MacMini, MacStudio, wolfe-pi5 — while the component's ritten.json declares its agents per node. A component that declares its agent runs on LAB_NODE's node.",
            required: false);

    public override string Name => "deploy";

    public override string Description => "Publishes the component's artifacts and converges this node's agents, retiring any they supersede.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveAgentDeclarations>(),
        Step.FromType<InstallAgentPackages>(),
        Step.FromType<ResolveAgents>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveArtifacts>(),
        Step.FromType<GateApproval>(),
        Step.FromType<PublishArtifacts>(),
        Step.FromType<ConvergeAgents>(),
        Step.FromType<DeclareAgentLogs>()
    ];

    public override IReadOnlyList<JobArgument> Arguments { get; } = [Node];

    public override JobKind Kind => JobKind.Deploy;

    protected override void Configure(IWorkflowBuilder builder, AgentsOptions options, JobArguments args)
    {
        base.Configure(builder, options);
        var runner = args.Get(Node) ?? "";
        var node = options.Nodes.GetValueOrDefault(runner) ?? new NodeAgentsOptions();
        builder.AddPackages().AddAgents().AddVolumes(node.Volumes).AddArtifacts(options.Artifacts);
        builder.Services.AddSingleton(new AgentsDeclaredPerNode(options.Nodes));
        builder.Services.AddSingleton(new NodeRunner(runner));
    }
}
