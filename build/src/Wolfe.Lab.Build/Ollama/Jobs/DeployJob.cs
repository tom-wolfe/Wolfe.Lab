using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Agents;
using Wolfe.Lab.Build.Agents.Models;
using Wolfe.Lab.Build.Agents.Steps;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Ollama.Models;
using Wolfe.Lab.Build.Ollama.Steps;
using Wolfe.Lab.Build.Paths;
using Wolfe.Lab.Build.Steps;

namespace Wolfe.Lab.Build.Ollama.Jobs;

/// <summary>
/// Converges the model server and what it holds.
/// </summary>
internal sealed class DeployJob : LabJob<OllamaSettings>
{
    private const string AgentName = "ollama";
    private const string StoreVariable = "OLLAMA_MODELS";

    public override string Name => "deploy";

    public override string Description => "Converges the model server's agent and pulls the models the slice declares.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveAgents>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<EnsureModelStore>(),
        Step.FromType<GateApproval>(),
        Step.FromType<ConvergeAgents>(),
        Step.FromType<ResolveModels>(),
        Step.FromType<PullModels>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<OllamaSettings> settings) => settings
        .Require(s => s.Agents.Count > 0, "'agents' names nothing in ritten.json.")
        .Require(s => s.Models.Store is not null, "'models.store' not set in ritten.json.")
        // One directory named in two places is a pair that drifts, and the way it fails is the
        // server starting against an empty store and answering with no models at all.
        .Require(
            s => s.Models.Store is { } store
                 && s.Agents.TryGetValue(AgentName, out var agent)
                 && agent.Environment.TryGetValue(StoreVariable, out var declared)
                 && HostPath.From(declared) == store,
            $"'models.store' must match the {AgentName} agent's {StoreVariable}.");

    protected override void Configure(IWorkflowBuilder builder, OllamaSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddAgents().AddOllama();
        builder.Services.AddSingleton(new AgentDeclarations(settings.Agents));
        builder.Services.AddSingleton(new RequiredVolumes([.. settings.Volumes.Select(volume => volume.Directory)]));
        builder.Services.AddSingleton(new ModelPlan([.. settings.Models.Pull]));

        if (settings.Models.Store is { } store)
        {
            builder.Services.AddSingleton(new ModelStore(store.Directory));
        }
    }
}
