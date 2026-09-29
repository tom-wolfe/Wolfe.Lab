using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Clients.Packages.Steps;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Clients.Releases.Steps;
using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Clients.Restic.Steps;
using Wolfe.Lab.Build.Clients.Volumes;
using Wolfe.Lab.Build.Clients.Volumes.Steps;
using Wolfe.Lab.Build.Workflows.Backup.Models;
using Wolfe.Lab.Build.Workflows.Backup.Steps;

namespace Wolfe.Lab.Build.Workflows.Backup.Jobs;

/// <summary>
/// Snapshots the state into the restic repository.
/// </summary>
internal sealed class BackupJob : LabJob<BackupOptions>
{
    public override string Name => "backup";

    public override string Description => "Snapshots the state into the restic repository, stopping the stack for the duration when one is named.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveRelease>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveImage>(),
        Step.FromType<TakeSnapshot>()
    ];

    public override JobKind Kind => JobKind.Work;

    protected override void ValidateSettings(SettingsValidator<BackupOptions> options) => options
        .Require(s => s.Release is { Length: > 0 }, "'release' not set in ritten.json: the name the snapshot is filed under.")
        .Require(s => s.Paths.Count > 0, "'paths' names nothing to snapshot.")
        .Require(s => s.Stop is null or { Length: > 0 }, "'stop' must name a container, or be left out for a warm snapshot.");

    protected override void Configure(IWorkflowBuilder builder, BackupOptions options)
    {
        base.Configure(builder, options);
        builder.AddDocker().AddRestic().AddVolumes(options.Volumes);
        builder.Services.AddSingleton(options.ToPlan());
        if (options.Release is { Length: > 0 } release)
        {
            builder.AddReleases(release);
        }
    }
}
