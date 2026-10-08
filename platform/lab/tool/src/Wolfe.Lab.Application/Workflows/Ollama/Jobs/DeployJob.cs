using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;
using Wolfe.Lab.Infrastructure.Ollama;
using Wolfe.Lab.Infrastructure.Packages;

namespace Wolfe.Lab.Application.Workflows.Ollama.Jobs;

/// <summary>
/// Deploys Ollama models to a node.
/// </summary>
internal sealed class DeployJob : LabJob<DeclaredSettings>
{
    public override string Name => "deploy";

    public override string Description => "Pulls the models this node's server serves and points each use's name at its model.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveServerPlan>(),
        Step.FromType<ResolveServerAgent>(),
        Step.FromType<InstallAgentPackages>(),
        Step.FromType<AwaitServer>(),
        Step.FromType<ResolveModels>(),
        Step.FromType<GateApproval>(),
        Step.FromType<PullModels>(),
        Step.FromType<PointUses>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddOllama().AddPackages();
    }
}
