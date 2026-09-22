using Ritten.OpenTofu;
using Ritten.OpenTofu.Steps;
using Wolfe.Lab.Build.Workflows.Common.Steps;
using Wolfe.Lab.Build.Workflows.Tofu.Models;
using Wolfe.Lab.Build.Workflows.Tofu.Steps;

namespace Wolfe.Lab.Build.Workflows.Tofu.Jobs;

/// <summary>
/// Plans the root on the pull request, so what a merge would apply is read before it is applied.
/// </summary>
internal sealed class CheckJob : LabJob<TofuSettings>
{
    public override string Name => "check";

    public override string Description => "Checks the root's formatting and plans it, so the change is read before it is applied.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<ResolveEnvironment>(),
        Step.FromType<TofuFormatCheck>(),
        Step.FromType<TofuInit>(),
        Step.FromType<TofuPlan>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, TofuSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddBuildReporting().AddOpenTofu();
    }
}
