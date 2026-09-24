using Microsoft.Extensions.DependencyInjection;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Releases;
using Ritten.Releases.Steps;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.DotNetTool.Models;
using Wolfe.Lab.Build.Workflows.DotNetTool.Steps;

namespace Wolfe.Lab.Build.Workflows.DotNetTool.Jobs;

/// <summary>
/// Packs the tool and pushes it to the lab's feed.
/// </summary>
internal sealed class DeployJob : LabJob<DotNetToolSettings>
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
        Step.FromType<AuthenticateFeed>(),
        Step.FromType<NugetPush>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<DotNetToolSettings> settings) => settings
        .Require(s => s.Project is { Length: > 0 }, "'project' not set in ritten.json.")
        .Require(s => s.Feed.Source is not null, "'feed.source' not set in ritten.json.")
        .Require(s => s.Feed.Token is not null, "'feed.token' not set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, DotNetToolSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddBuildReporting()
            .AddDotNet([settings.Project!], settings.Configuration)
            .AddNuGet(settings.Feed.Source!.ToString(), ReleaseLine.Major, ReleaseCadence.Continuous);
        builder.Services.AddSingleton(new FeedToken(settings.Feed.Token!.Value));
    }
}
