using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Restic;
using Wolfe.Lab.Application.Volumes;
using Wolfe.Lab.Application.Workflows.Backup.Models;
using Wolfe.Lab.Application.Workflows.Backup.Steps;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Infrastructure.Restic;
using Wolfe.Lab.Infrastructure.Volumes;

namespace Wolfe.Lab.Application.Workflows.Backup.Jobs;

/// <summary>
/// Proves the latest snapshot restores, without touching the live state.
/// </summary>
/// <remarks>
/// A backup nobody has restored from has not shipped. This is the weekly half of that: the
/// paths that prove a restore, brought back into scratch and asserted non-empty. That the
/// service boots on them is the quarterly half, done by hand with <c>restore</c>.
/// </remarks>
internal sealed class DrillJob : LabJob<BackupOptions>
{
    public override string Name => "restore-drill";

    public override string Description => "Restores what proves the latest snapshot into a scratch directory and asserts it came back.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveRelease>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<DrillRestore>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<BackupOptions> options) => options
        .Require(s => s.Release is { Length: > 0 }, "'release' not set in ritten.json: the name the snapshot is filed under.")
        .Require(s => s.Paths.Count > 0, "'paths' names nothing to snapshot.")
        .Require(s => s.Verify.Count > 0, "'verify' names nothing that proves a restore.");

    protected override void Configure(IWorkflowBuilder builder, BackupOptions options)
    {
        base.Configure(builder, options);
        builder.AddRestic().AddVolumes(options.Volumes);
        builder.Services.AddSingleton(options.ToPlan());
        if (options.Release is { Length: > 0 } release)
        {
            builder.AddReleases(release);
        }
    }
}
