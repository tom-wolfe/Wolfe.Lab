using Ritten.Docker;
using Ritten.Docker.Steps;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.Docker.Models;

namespace Wolfe.Lab.Build.Workflows.Docker.Jobs;

/// <summary>
/// Proves the component's stack is sound before anything is deployed from it.
/// </summary>
/// <remarks>
/// Reads the checkout, not a release: a check runs before the merge, when nothing is installed.
/// Secrets are absent there and that is fine — they reach compose through its environment, and
/// an interpolation with nothing behind it resolves empty rather than failing.
/// </remarks>
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
