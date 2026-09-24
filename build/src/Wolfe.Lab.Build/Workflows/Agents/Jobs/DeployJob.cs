using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Clients.Agents.Steps;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Clients.Volumes;
using Wolfe.Lab.Build.Clients.Volumes.Steps;
using Wolfe.Lab.Build.Workflows.Agents.Models;

namespace Wolfe.Lab.Build.Workflows.Agents.Jobs;

/// <summary>
/// Converges one node's agents.
/// </summary>
internal sealed class DeployJob : LabJob<AgentsSettings>
{
    private static readonly JobArgument<string> Node =
        JobArgument.Value<string>("node", "The node this runs on, as ritten.json names it: MacMini, MacStudio, wolfe-pi5.", required: true);

    public override string Name => "deploy";

    public override string Description => "Converges this node's agents, retiring any they supersede.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveAgents>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<GateApproval>(),
        Step.FromType<ConvergeAgents>()
    ];

    public override IReadOnlyList<JobArgument> Arguments { get; } = [Node];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<AgentsSettings> settings) => settings
        .Require(s => s.Nodes.Count > 0, "'nodes' names no node in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, AgentsSettings settings, JobArguments args)
    {
        base.Configure(builder, settings);
        var node = settings.Nodes.GetValueOrDefault(args.Get(Node)!) ?? new NodeAgentsSettings();
        builder.AddAgents().AddVolumes(node.Volumes);
        builder.Services.AddSingleton(new AgentDeclarations(node.Agents));
    }
}
