using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Heartbeat;
using Wolfe.Lab.Build.Clients.Heartbeat.Steps;
using Wolfe.Lab.Build.Workflows.Heartbeat.Models;

namespace Wolfe.Lab.Build.Workflows.Heartbeat.Jobs;

/// <summary>
/// One ping. The schedule is the workflow's; the check's expectation of it is declared in
/// <c>tofu/</c>, so a slot this job misses is what healthchecks.io shouts about.
/// </summary>
internal sealed class PingJob : LabJob<HeartbeatSettings>
{
    public override string Name => "ping";

    public override string Description => "Pings the lab's healthchecks.io check: proof the scheduler is alive.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<PingHeartbeat>()];

    public override JobKind Kind => JobKind.Work;

    protected override void ValidateSettings(SettingsValidator<HeartbeatSettings> settings) => settings
        .Require(s => s.Heartbeat.ToCheck() is not null, "'heartbeat.check' and 'heartbeat.key' must both be set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, HeartbeatSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddHeartbeat();
        if (settings.Heartbeat.ToCheck() is { } check)
        {
            builder.Services.AddSingleton(check);
        }
    }
}
