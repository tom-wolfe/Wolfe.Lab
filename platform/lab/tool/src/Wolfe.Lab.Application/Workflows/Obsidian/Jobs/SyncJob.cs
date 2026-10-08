using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Workflows.Obsidian.Steps;
using Wolfe.Lab.Infrastructure.Obsidian;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Jobs;

internal sealed class SyncJob : LabJob<DeclaredSettings>
{
    public override string Name => "sync";

    public override string Description =>
        "Pulls the vault from Obsidian Sync, commits what changed and pushes it to Forgejo.";

    public override JobKind Kind => JobKind.Work;

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveVault>(),
        Step.FromType<SyncVault>(),
        Step.FromType<CommitVault>(),
        Step.FromType<GateApproval>(),
        Step.FromType<PushVault>()
    ];

    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddObsidian();
    }
}
