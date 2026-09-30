using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Clients.Agents;
using Wolfe.Lab.Clients.Agents.Steps;
using Wolfe.Lab.Clients.Gates.Steps;
using Wolfe.Lab.Clients.Packages;
using Wolfe.Lab.Clients.Packages.Steps;
using Wolfe.Lab.Clients.Releases;
using Wolfe.Lab.Clients.Releases.Steps;
using Wolfe.Lab.Clients.Volumes;
using Wolfe.Lab.Clients.Volumes.Steps;
using Wolfe.Lab.Workflows.Agents.Models;

namespace Wolfe.Lab.Workflows.Agents.Jobs;

/// <summary>
/// Converges one node's agents.
/// </summary>
internal sealed class DeployJob : LabJob<AgentsOptions>
{
    private static readonly JobArgument<string> Node =
        JobArgument.Value<string>("node", "The node this runs on, as ritten.json names it: MacMini, MacStudio, wolfe-pi5.", required: true);

    public override string Name => "deploy";

    public override string Description => "Publishes the component's artifacts and converges this node's agents, retiring any they supersede.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveComponent>(),
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

    protected override void ValidateSettings(SettingsValidator<AgentsOptions> options) => options
        .Require(s => s.Nodes.Count > 0, "'nodes' names no node in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, AgentsOptions options, JobArguments args)
    {
        base.Configure(builder, options);
        var node = (args.Get(Node) is { } name ? options.Nodes.GetValueOrDefault(name) : null) ?? new NodeAgentsOptions();
        builder.AddPackages().AddAgents().AddVolumes(node.Volumes).AddArtifacts(options.Artifacts);
        builder.Services.AddSingleton(new AgentDeclarations(node.Agents));
    }
}
