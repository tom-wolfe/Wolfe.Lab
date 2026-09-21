using Ritten.DotNet;
using Wolfe.Lab.Build.DotNet.Models;
using Wolfe.Lab.Build.DotNet.Steps;
using Wolfe.Lab.Build.Steps;

namespace Wolfe.Lab.Build.DotNet.Jobs;

/// <summary>
/// Proves the component's code is sound before anything is built from it.
/// </summary>
internal sealed class CheckJob : LabJob<DotNetServiceSettings>
{
    public override string Name => "check";

    public override string Description => "Restores, verifies formatting, builds and tests the component.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetFormatCheck>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, DotNetServiceSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddBuildReporting().AddDotNet([], settings.Configuration);
    }
}
