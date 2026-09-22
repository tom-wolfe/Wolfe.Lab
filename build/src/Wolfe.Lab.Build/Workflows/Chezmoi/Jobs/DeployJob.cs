using Wolfe.Lab.Build.Clients.Chezmoi;
using Wolfe.Lab.Build.Workflows.Chezmoi.Models;
using Wolfe.Lab.Build.Workflows.Chezmoi.Steps;
using Wolfe.Lab.Build.Workflows.Common.Steps;

namespace Wolfe.Lab.Build.Workflows.Chezmoi.Jobs;

/// <summary>
/// Makes this node match the merged source. Run on each node by the workflow's matrix.
/// </summary>
internal sealed class DeployJob : LabJob<ChezmoiSettings>
{
    public override string Name => "deploy";

    public override string Description => "Runs chezmoi update on this node.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GateApproval>(),
        Step.FromType<UpdateNode>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void Configure(IWorkflowBuilder builder, ChezmoiSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddChezmoi();
    }
}
