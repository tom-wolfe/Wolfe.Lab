using Microsoft.Extensions.DependencyInjection;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Git;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Releases;
using Ritten.Releases.Steps;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Workflows.DotNetTool.Models;
using Wolfe.Lab.Application.Workflows.DotNetTool.Steps;

namespace Wolfe.Lab.Application.Workflows.DotNetTool.Jobs;

/// <summary>
/// Packs the tool and pushes it to the lab's feed.
/// </summary>
internal sealed class DeployJob : LabJob<DotNetToolOptions>
{
    public override string Name => "deploy";

    public override string Description => "Packs the tool and pushes it to the feed, unless the feed already has this version.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ComputeVersion>(),
        Step.FromType<ReadProjects>(),
        Step.FromType<ResolveRelease>(),
        Step.FromType<NugetRead>(),
        Step.FromType<ReleasableGate>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>(),
        Step.FromType<DotnetPack>(),
        Step.FromType<GateApproval>(),
        Step.FromType<TagRelease>(),
        Step.FromType<NugetAuthenticate>(),
        Step.FromType<NugetPush>()
    ];

    /// <summary>
    /// A published CLI's tag, before its version: <c>lab/v1.0.214</c>, apart from any other tag.
    /// </summary>
    internal const string TagPrefix = "lab/v";

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<DotNetToolOptions> options) => options
        .Require(s => s.Project is { Length: > 0 }, "'project' not set in ritten.json.")
        .Require(s => s.Feed.Source is not null, "'feed.source' not set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, DotNetToolOptions options)
    {
        base.Configure(builder, options);
        builder.AddBuildReporting().AddGit(TagPrefix);
        if (options is { Project: { Length: > 0 } project, Feed.Source: { } source })
        {
            builder.Services.AddSingleton(ShippedInputs.For(project));
            builder.AddDotNet([project], options.Configuration)
                .AddNuGet(source.ToString(), ReleaseLine.Major, ReleaseCadence.Continuous);
        }
    }
}
