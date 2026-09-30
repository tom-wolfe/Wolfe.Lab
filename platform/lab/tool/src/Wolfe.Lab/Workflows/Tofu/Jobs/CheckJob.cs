using Ritten.OpenTofu;
using Ritten.OpenTofu.Steps;
using Wolfe.Lab.Clients.Gates.Steps;
using Wolfe.Lab.Clients.Packages;
using Wolfe.Lab.Clients.Packages.Steps;
using Wolfe.Lab.Workflows.Tofu.Models;
using Wolfe.Lab.Workflows.Tofu.Steps;

namespace Wolfe.Lab.Workflows.Tofu.Jobs;

/// <summary>
/// Plans the root on the pull request, so what a merge would apply is read before it is applied.
/// </summary>
internal sealed class CheckJob : LabJob<TofuOptions>
{
    public override string Name => "check";

    public override string Description => "Checks the root's formatting and plans it, so the change is read before it is applied.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveEnvironment>(),
        Step.FromType<TofuFormatCheck>(),
        Step.FromType<TofuInit>(),
        Step.FromType<TofuPlan>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, TofuOptions options)
    {
        base.Configure(builder, options);
        builder.AddBuildReporting().AddOpenTofu().AddTools("tofu");
    }
}
