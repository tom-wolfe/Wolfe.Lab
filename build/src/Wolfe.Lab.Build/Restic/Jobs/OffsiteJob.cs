using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Backup.Steps;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Heartbeat;
using Wolfe.Lab.Build.Heartbeat.Steps;
using Wolfe.Lab.Build.Restic.Models;
using Wolfe.Lab.Build.Restic.Steps;

namespace Wolfe.Lab.Build.Restic.Jobs;

/// <summary>
/// The second half of the backup design, copying to B2.
/// </summary>
internal sealed class OffsiteJob : ResticJob
{
    public override string Name => "offsite";

    public override string Description => "Copies new snapshots to the offsite repository, applies retention to both, and pings the dead man's switch.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveSlice>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveOffsite>(),
        Step.FromType<CopyOffsite>(),
        Step.FromType<ApplyRetention>(),
        Step.FromType<PingHeartbeat>()
    ];

    public override JobKind Kind => JobKind.Work;

    protected override void ValidateSettings(SettingsValidator<ResticSettings> settings) => settings
        .Require(s => s.Retention is { Daily: >= 0, Weekly: >= 0, Monthly: >= 0 }, "'retention' counts cannot be negative.")
        .Require(s => s.Retention.Daily + s.Retention.Weekly + s.Retention.Monthly > 0, "'retention' keeps nothing: every snapshot would be pruned.")
        .Require(s => s.Heartbeat.ToCheck() is not null, "'heartbeat.check' and 'heartbeat.key' must both be set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, ResticSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddHeartbeat();
        builder.Services.AddSingleton(new RetentionPolicy(settings.Retention.Daily, settings.Retention.Weekly, settings.Retention.Monthly, settings.Retention.KeepTags));
        if (settings.Heartbeat.ToCheck() is { } check)
        {
            builder.Services.AddSingleton(check);
        }
    }
}
