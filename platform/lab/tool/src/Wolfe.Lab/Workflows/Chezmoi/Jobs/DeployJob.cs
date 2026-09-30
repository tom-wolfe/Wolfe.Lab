using Wolfe.Lab.Clients.Chezmoi;
using Wolfe.Lab.Clients.Gates.Steps;
using Wolfe.Lab.Clients.Packages.Steps;
using Wolfe.Lab.Workflows.Chezmoi.Models;
using Wolfe.Lab.Workflows.Chezmoi.Steps;

namespace Wolfe.Lab.Workflows.Chezmoi.Jobs;

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
