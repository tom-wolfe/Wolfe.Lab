using Ritten.Docker;
using Wolfe.Lab.Build.Workflows.Common.Steps;
using Wolfe.Lab.Build.Workflows.Docker.Models;
using Wolfe.Lab.Build.Workflows.Docker.Steps;

namespace Wolfe.Lab.Build.Workflows.Docker.Jobs;

/// <summary>
/// Proves the component's stack is sound before anything is deployed from it.
/// </summary>
internal sealed class CheckJob<TSettings> : LabJob<TSettings> where TSettings : DockerSettings
{
    public override string Name => "check";

    public override string Description => "Checks that compose can read the component's stack.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<ComposeCheck>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, TSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker();
    }
}
