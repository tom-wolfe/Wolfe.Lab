using Microsoft.Extensions.DependencyInjection;
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
/// Proves the latest snapshot restores, without touching the live state.
/// </summary>
/// <remarks>
/// A backup nobody has restored from has not shipped. This is the weekly half of that: the
/// paths that prove a restore, brought back into scratch and asserted non-empty. That the
/// service boots on them is the quarterly half, done by hand with <c>restore</c>.
/// </remarks>
internal sealed class DrillJob : LabJob<BackupSettings>
{
    public override string Name => "restore-drill";

    public override string Description => "Restores what proves the latest snapshot into a scratch directory and asserts it came back.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveRelease>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<DrillRestore>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<BackupSettings> settings) => settings
        .Require(s => s.Release is { Length: > 0 }, "'release' not set in ritten.json: the name the snapshot is filed under.")
        .Require(s => s.Paths.Count > 0, "'paths' names nothing to snapshot.")
        .Require(s => s.Verify.Count > 0, "'verify' names nothing that proves a restore.");

    protected override void Configure(IWorkflowBuilder builder, BackupSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddRestic().AddVolumes(settings.Volumes);
        builder.Services.AddSingleton(settings.ToPlan());
        if (settings.Release is { Length: > 0 } release)
        {
            builder.AddReleases(release);
        }
    }
}
