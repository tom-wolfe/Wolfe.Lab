using Ritten.Docker;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Restic;
using Wolfe.Lab.Application.Volumes;
using Wolfe.Lab.Application.Workflows.Backup.Steps;
using Wolfe.Lab.Infrastructure.Restic;

namespace Wolfe.Lab.Application.Workflows.Backup.Jobs;

/// <summary>
/// Snapshots the state into the restic repository.
/// </summary>
internal sealed class BackupJob : LabJob<DeclaredSettings>
{
    public override string Name => "backup";

    public override string Description => "Snapshots the state into the restic repository, stopping the stack for the duration when one is named.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveBackupPlan>(),
        Step.FromType<ResolveImage>(),
        Step.FromType<TakeSnapshot>()
    ];

    public override JobKind Kind => JobKind.Work;

    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddDocker().AddRestic();
    }
}
