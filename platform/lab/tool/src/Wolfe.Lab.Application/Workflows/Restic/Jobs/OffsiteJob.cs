using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Heartbeat;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Restic;
using Wolfe.Lab.Application.Volumes;
using Wolfe.Lab.Application.Workflows.Restic.Steps;
using Wolfe.Lab.Infrastructure.Heartbeat;

namespace Wolfe.Lab.Application.Workflows.Restic.Jobs;

/// <summary>
/// The second half of the backup design, copying to B2.
/// </summary>
internal sealed class OffsiteJob : ResticJob
{
    public override string Name => "offsite";

    public override string Description => "Copies new snapshots to the offsite repository, applies retention to both, and pings the dead man's switch.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveRepositories>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveOffsite>(),
        Step.FromType<CopyOffsite>(),
        Step.FromType<ApplyRetention>(),
        Step.FromType<ResolveHeartbeat>(),
        Step.FromType<PingHeartbeat>()
    ];

    public override JobKind Kind => JobKind.Work;


    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddHeartbeat();
    }
}
