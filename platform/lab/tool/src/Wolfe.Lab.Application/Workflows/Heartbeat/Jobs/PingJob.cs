using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Heartbeat;
using Wolfe.Lab.Application.Releases;
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

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveHeartbeat>(),
        Step.FromType<PingHeartbeat>()
    ];

    public override JobKind Kind => JobKind.Work;


    protected override void Configure(IWorkflowBuilder builder, HeartbeatOptions options)
    {
        base.Configure(builder, options);
        builder.AddHeartbeat();

        // Until the ping declares its heartbeat, its ritten.json says it.
        builder.Services.AddSingleton(options.Heartbeat);
    }
}
