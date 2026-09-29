using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Releases;
using Ritten.Releases.Steps;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.DotNetTool.Models;

namespace Wolfe.Lab.Build.Workflows.DotNetTool.Jobs;

/// <summary>
/// Packs the tool and pushes it to the lab's feed.
/// </summary>
internal sealed class DeployJob : LabJob<DotNetToolOptions>
{
    public override string Name => "deploy";

    public override string Description => "Packs the tool and pushes it to the feed, unless the feed already has this version.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ReadProjects>(),
        Step.FromType<ResolveRelease>(),
        Step.FromType<ReadShippedChanges>(),
        Step.FromType<NugetRead>(),
        Step.FromType<CheckVersion>(),
        Step.FromType<ReleasableGate>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>(),
        Step.FromType<DotnetPack>(),
        Step.FromType<GateApproval>(),
        Step.FromType<NugetAuthenticate>(),
        Step.FromType<NugetPush>()
    ];

    public override JobKind Kind => JobKind.Deploy;

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
