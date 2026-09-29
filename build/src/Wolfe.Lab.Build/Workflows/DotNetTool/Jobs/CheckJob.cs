using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Releases;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.DotNetTool.Models;

namespace Wolfe.Lab.Build.Workflows.DotNetTool.Jobs;

/// <summary>
/// Proves the tool is sound, and that a change to it will actually ship.
/// </summary>
/// <remarks>
/// The cadence is continuous: the merge publishes, so a pull request that changes what ships must move
/// <c>&lt;Version&gt;</c> — otherwise the merge would publish nothing and the pins would sit on a package that no
/// longer matches its source.
/// </remarks>
internal sealed class CheckJob : LabJob<DotNetToolOptions>
{
    public override string Name => "check";

    public override string Description => "Restores, verifies formatting, builds and tests the tool, and checks its version moved.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetFormatCheck>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>(),
        Step.FromType<ReadProjects>(),
        Step.FromType<ResolveRelease>(),
        Step.FromType<ReadShippedChanges>(),
        Step.FromType<NugetRead>(),
        Step.FromType<CheckVersion>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<DotNetToolOptions> options) => options
        .Require(s => s.Project is { Length: > 0 }, "'project' not set in ritten.json.")
        .Require(s => s.Feed.Source is not null, "'feed.source' not set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, DotNetToolOptions options)
    {
        base.Configure(builder, options);
        builder.AddBuildReporting();
        if (options.Project is { Length: > 0 } project && options.Feed.Source is { } source)
        {
            builder.AddDotNet([project], options.Configuration)
                .AddNuGet(source.ToString(), ReleaseLine.Major, ReleaseCadence.Continuous);
        }
    }
}
