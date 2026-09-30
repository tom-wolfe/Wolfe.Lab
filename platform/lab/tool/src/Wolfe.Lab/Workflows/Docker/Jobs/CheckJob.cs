using Ritten.Docker;
using Ritten.Docker.Steps;
using Wolfe.Lab.Clients.Gates.Steps;
using Wolfe.Lab.Clients.Telemetry.Steps;
using Wolfe.Lab.Workflows.Docker.Models;

namespace Wolfe.Lab.Workflows.Docker.Jobs;

/// <summary>
/// Proves the component's stack is sound before anything is deployed from it.
/// </summary>
internal sealed class CheckJob<TOptions> : LabJob<TOptions> where TOptions : DockerComponentOptions
{
    public override string Name => "check";

    public override string Description => "Checks that compose can read the component's stack.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<ComposeCheck>(),
        Step.FromType<CheckTelemetryNames>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, TOptions options)
    {
        base.Configure(builder, options);
        builder.AddDocker();
    }
}
