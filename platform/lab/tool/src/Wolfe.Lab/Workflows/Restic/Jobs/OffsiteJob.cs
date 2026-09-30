using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Clients.Heartbeat;
using Wolfe.Lab.Clients.Heartbeat.Steps;
using Wolfe.Lab.Clients.Packages.Steps;
using Wolfe.Lab.Clients.Restic.Steps;
using Wolfe.Lab.Clients.Volumes.Steps;
using Wolfe.Lab.Workflows.Restic.Models;
using Wolfe.Lab.Workflows.Restic.Steps;

namespace Wolfe.Lab.Workflows.Restic.Jobs;

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
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveOffsite>(),
        Step.FromType<CopyOffsite>(),
        Step.FromType<ApplyRetention>(),
        Step.FromType<PingHeartbeat>()
    ];

    public override JobKind Kind => JobKind.Work;

    protected override void ValidateSettings(SettingsValidator<ResticOptions> options) => options
        .Require(s => s.Retention is { Daily: >= 0, Weekly: >= 0, Monthly: >= 0 }, "'retention' counts cannot be negative.")
        .Require(s => s.Retention.Daily + s.Retention.Weekly + s.Retention.Monthly > 0, "'retention' keeps nothing: every snapshot would be pruned.")
        .Require(s => s.Heartbeat.ToCheck() is not null, "'heartbeat.check' and 'heartbeat.key' must both be set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, ResticOptions options)
    {
        base.Configure(builder, options);
        builder.AddHeartbeat();
        builder.Services.AddSingleton(new RetentionPolicy(options.Retention.Daily, options.Retention.Weekly, options.Retention.Monthly, options.Retention.KeepTags));
        if (options.Heartbeat.ToCheck() is { } check)
        {
            builder.Services.AddSingleton(check);
        }
    }
}
