using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Clients.Agents.Steps;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Clients.Ollama;
using Wolfe.Lab.Build.Clients.Packages;
using Wolfe.Lab.Build.Clients.Packages.Steps;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Clients.Releases.Steps;
using Wolfe.Lab.Build.Clients.Volumes;
using Wolfe.Lab.Build.Clients.Volumes.Steps;
using Wolfe.Lab.Build.Values;
using Wolfe.Lab.Build.Workflows.Ollama.Models;
using Wolfe.Lab.Build.Workflows.Ollama.Steps;

namespace Wolfe.Lab.Build.Workflows.Ollama.Jobs;

/// <summary>
/// Converges the model server and what it holds.
/// </summary>
internal sealed class DeployJob : LabJob<OllamaOptions>
{
    private const string AgentName = "ollama";
    private const string StoreVariable = "OLLAMA_MODELS";

    public override string Name => "deploy";

    public override string Description => "Converges the model server's agent, pulls the models the slice declares and points its roles at them.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<CheckRoles>(),
        Step.FromType<ResolveComponent>(),
        Step.FromType<InstallAgentPackages>(),
        Step.FromType<ResolveAgents>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveArtifacts>(),
        Step.FromType<GateApproval>(),
        Step.FromType<EnsureModelStore>(),
        Step.FromType<PublishArtifacts>(),
        Step.FromType<ConvergeAgents>(),
        Step.FromType<DeclareAgentLogs>(),
        Step.FromType<AwaitServer>(),
        Step.FromType<ResolveModels>(),
        Step.FromType<PullModels>(),
        Step.FromType<PointRoles>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<OllamaOptions> options) => options
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

    protected override void Configure(IWorkflowBuilder builder, OllamaOptions options)
    {
        base.Configure(builder, options);
        builder.AddPackages().AddAgents().AddOllama().AddVolumes(options.Volumes).AddArtifacts([]);
        builder.Services.AddSingleton(new AgentDeclarations(options.Agents));
        builder.Services.AddSingleton(new ModelPlan([.. options.Models.Pull]));
        builder.Services.AddSingleton(RolePlan.From(options.Models.Roles));
        builder.Services.AddSingleton(new DeclaredRoles(options.Models));

        if (options.Models.Store is { } store)
        {
            builder.Services.AddSingleton(new ModelStore(store.Directory));
        }
    }
}
