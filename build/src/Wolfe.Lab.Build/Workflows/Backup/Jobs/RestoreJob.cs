using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Clients.Gates.Steps;
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
/// Puts a snapshot back in place of the live state.
/// </summary>
/// <remarks>
/// The live state is set aside rather than deleted, so a restore that turns out wrong is undone
/// by moving it back. The stack is held to the image the snapshot was taken under, because a
/// database written by one version and opened by another is the failure that looks like success.
/// </remarks>
internal sealed class RestoreJob : LabJob<BackupSettings>
{
    internal static readonly JobArgument<string> Snapshot = JobArgument.Value<string>(
        "snapshot",
        "The snapshot to restore, by id. The latest when left out.");

    internal static readonly JobArgument<bool> AnyImage = JobArgument.Value<bool>(
        "any-image",
        "Restore onto whatever image the stack runs, rather than the one the snapshot was taken under.");

    public override string Name => "restore";

    public override string Description => "Stops the stack, sets its state aside, restores a snapshot in its place and starts it again.";

    public override IReadOnlyList<JobArgument> Arguments { get; } = [Snapshot, AnyImage];

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveRelease>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveSnapshot>(),
        Step.FromType<ResolveImage>(),
        Step.FromType<CheckImage>(),
        Step.FromType<GateApproval>(),
        Step.FromType<RestoreState>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<BackupSettings> settings) => settings
        .Require(s => s.Release is { Length: > 0 }, "'release' not set in ritten.json: the name the snapshot is filed under.")
        .Require(s => s.Paths.Count > 0, "'paths' names nothing to restore.");

    protected override void Configure(IWorkflowBuilder builder, BackupSettings settings, JobArguments args)
    {
        base.Configure(builder, settings, args);
        builder.AddDocker().AddRestic().AddStateDirectories().AddVolumes(settings.Volumes);
        builder.Services.AddSingleton(settings.ToPlan());
        builder.Services.AddSingleton(new RestoreRequest(args.Get(Snapshot), args.Get(AnyImage)));
        if (settings.Release is { Length: > 0 } release)
        {
            builder.AddReleases(release);
        }
    }
}
