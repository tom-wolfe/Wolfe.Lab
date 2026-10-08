using Ritten.OpenTofu;
using Ritten.OpenTofu.Steps;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Workflows.Tofu.Steps;
using Wolfe.Lab.Infrastructure.Packages;

namespace Wolfe.Lab.Application.Workflows.Tofu.Jobs;

/// <summary>
/// Plans the root on the pull request, so what a merge would apply is read before it is applied.
/// </summary>
internal sealed class CheckJob : LabJob<DeclaredSettings>
{
    public override string Name => "check";

    public override string Description => "Checks the root's formatting and plans it, so the change is read before it is applied.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveEnvironment>(),
        Step.FromType<TofuFormatCheck>(),
        Step.FromType<TofuInit>(),
        Step.FromType<TofuPlan>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddBuildReporting().AddOpenTofu().AddTools("tofu");
    }
}
