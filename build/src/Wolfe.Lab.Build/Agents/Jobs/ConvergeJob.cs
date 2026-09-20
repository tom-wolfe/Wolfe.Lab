using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Agents.Models;
using Wolfe.Lab.Build.Agents.Steps;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Steps;

namespace Wolfe.Lab.Build.Agents.Jobs;

/// <summary>
/// Deploys the slice's supervised agents.
/// </summary>
internal sealed class ConvergeJob : LabJob<AgentsSettings>
{
    public override string Name => "converge";

    public override string Description => "Makes the node's supervised agents match what the slice declares.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveAgents>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ApprovalGate>(),
        Step.FromType<ConvergeAgents>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<AgentsSettings> settings) => settings
        .Require(s => s.Agents.Count > 0, "'agents' names nothing in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, AgentsSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddAgents();
        builder.Services.AddSingleton(new AgentDeclarations(settings.Agents));
        builder.Services.AddSingleton(new RequiredVolumes([.. settings.Volumes.Select(volume => volume.Directory)]));
    }
}
