using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Heartbeat;
using Wolfe.Lab.Application.Workflows.Heartbeat.Models;
using Wolfe.Lab.Infrastructure.Heartbeat;

namespace Wolfe.Lab.Application.Workflows.Heartbeat.Jobs;

/// <summary>
/// One ping. The schedule is the workflow's; the check's expectation of it is declared in
/// <c>tofu/</c>, so a slot this job misses is what healthchecks.io shouts about.
/// </summary>
internal sealed class PingJob : LabJob<HeartbeatOptions>
{
    public override string Name => "ping";

    public override string Description => "Pings the lab's healthchecks.io check: proof the scheduler is alive.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<PingHeartbeat>()];

    public override JobKind Kind => JobKind.Work;

    protected override void ValidateSettings(SettingsValidator<HeartbeatOptions> options) => options
        .Require(s => s.Heartbeat.ToCheck() is not null, "'heartbeat.check' and 'heartbeat.key' must both be set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, HeartbeatOptions options)
    {
        base.Configure(builder, options);
        builder.AddHeartbeat();
        if (options.Heartbeat.ToCheck() is { } check)
        {
            builder.Services.AddSingleton(check);
        }
    }
}
