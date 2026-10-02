using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Workflows.Chezmoi.Models;
using Wolfe.Lab.Application.Workflows.Chezmoi.Steps;
using Wolfe.Lab.Infrastructure.Chezmoi;

namespace Wolfe.Lab.Application.Workflows.Chezmoi.Jobs;

/// <summary>
/// Makes this node match the merged source. Run on each node by the workflow's matrix.
/// </summary>
internal sealed class DeployJob : LabJob<ChezmoiOptions>
{
    public override string Name => "deploy";

    public override string Description => "Runs chezmoi update on this node.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<EnsureTools>(),
        Step.FromType<GateApproval>(),
        Step.FromType<UpdateNode>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void Configure(IWorkflowBuilder builder, ChezmoiOptions options)
    {
        base.Configure(builder, options);
        builder.AddChezmoi();
    }
}
