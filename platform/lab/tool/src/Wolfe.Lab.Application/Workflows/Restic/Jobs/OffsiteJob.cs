using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Heartbeat;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Restic;
using Wolfe.Lab.Application.Volumes;
using Wolfe.Lab.Application.Workflows.Restic.Models;
using Wolfe.Lab.Application.Workflows.Restic.Steps;
using Wolfe.Lab.Domain.Backups;
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
