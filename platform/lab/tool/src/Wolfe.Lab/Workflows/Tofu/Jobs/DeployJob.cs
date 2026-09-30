using Ritten.OpenTofu;
using Ritten.OpenTofu.Steps;
using Wolfe.Lab.Clients.Gates.Steps;
using Wolfe.Lab.Clients.Packages;
using Wolfe.Lab.Clients.Packages.Steps;
using Wolfe.Lab.Workflows.Tofu.Models;
using Wolfe.Lab.Workflows.Tofu.Steps;

namespace Wolfe.Lab.Workflows.Tofu.Jobs;

/// <summary>
/// Applies the root: plans, and applies only when the plan found something to do.
/// </summary>
internal sealed class DeployJob : LabJob<TofuOptions>
{
    public override string Name => "deploy";

    public override string Description => "Plans the root and applies what the plan found.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveEnvironment>(),
        Step.FromType<TofuInit>(),
        Step.FromType<TofuPlan>(),
        Step.FromType<GateApproval>(),
        Step.FromType<TofuApply>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void Configure(IWorkflowBuilder builder, TofuOptions options)
    {
        base.Configure(builder, options);
        builder.AddBuildReporting().AddOpenTofu().AddTools("tofu");
    }
}
