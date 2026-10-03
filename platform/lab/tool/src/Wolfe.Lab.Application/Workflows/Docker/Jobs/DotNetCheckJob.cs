using Ritten.Docker;
using Ritten.Docker.Steps;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Workflows.Docker.Models;

namespace Wolfe.Lab.Application.Workflows.Docker.Jobs;

/// <summary>
/// Proves the component is sound before anything is built or deployed from it.
/// </summary>
internal sealed class DotNetCheckJob : LabJob<DotNetServiceOptions>
{
    public override string Name => "check";

    public override string Description => "Restores, verifies formatting, builds and tests the component.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<CheckServiceCatalog>(),
        Step.FromType<ComposeCheck>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetFormatCheck>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, DotNetServiceOptions options)
    {
        base.Configure(builder, options);
        builder.AddDocker().AddBuildReporting().AddDotNet([], options.Configuration);
    }
}
