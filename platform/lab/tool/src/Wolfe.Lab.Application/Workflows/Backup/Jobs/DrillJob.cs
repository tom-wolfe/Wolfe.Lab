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
/// Proves the latest snapshot restores, without touching the live state.
/// </summary>
/// <remarks>
/// A backup nobody has restored from has not shipped. This is the weekly half of that: the
/// paths that prove a restore, brought back into scratch and asserted non-empty. That the
/// service boots on them is the quarterly half, done by hand with <c>restore</c>.
/// </remarks>
internal sealed class DrillJob : LabJob<DeclaredSettings>
{
    public override string Name => "restore-drill";

    public override string Description => "Restores what proves the latest snapshot into a scratch directory and asserts it came back.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveBackupPlan>(),
        Step.FromType<DrillRestore>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddDocker().AddRestic();
    }
}
