using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Volumes;
using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Ollama;
using Wolfe.Lab.Infrastructure.Packages;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Infrastructure.Volumes;

namespace Wolfe.Lab.Application.Workflows.Ollama.Jobs;

/// <summary>
/// Converges the model server and what it holds.
/// </summary>
internal sealed class DeployJob : LabJob<OllamaOptions>
{
    public override string Name => "deploy";

    public override string Description => "Converges the model server's agent, pulls the models the service declares and points its roles at them.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<CheckRoles>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveOllamaAgents>(),
        Step.FromType<CheckModelStore>(),
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
        .Require(s => s.Models.Store is not null, "'models.store' not set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, OllamaOptions options)
    {
        base.Configure(builder, options);
        builder.AddPackages().AddAgents().AddOllama().AddVolumes(options.Volumes).AddArtifacts([]);
        builder.Services.AddSingleton(new OllamaAgents(options.Agents));
        builder.Services.AddSingleton(new ModelPlan([.. options.Models.Pull]));
        builder.Services.AddSingleton(RolePlan.From(options.Models.Roles));
        builder.Services.AddSingleton(new DeclaredRoles(options.Models));

        if (options.Models.Store is { } store)
        {
            builder.Services.AddSingleton(new ModelStore(store.Directory));
        }
    }
}
