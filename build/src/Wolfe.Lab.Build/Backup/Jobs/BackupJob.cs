using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Backup.Steps;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Restic;

namespace Wolfe.Lab.Build.Backup.Jobs;

/// <summary>
/// Snapshots a slice into the lab's restic repository: the one pipeline every slice's nightly
/// backup runs, with the slice's own <c>backup</c> settings as the only variation.
/// </summary>
/// <typeparam name="TSettings">The slice's <c>ritten.json</c> shape, which carries the <c>backup</c> section.</typeparam>
internal sealed class BackupJob<TSettings> : LabJob<TSettings> where TSettings : SliceSettings
{
    public override string Name => "backup";

    public override string Description => "Snapshots the slice's state into the restic repository, stopping the stack for the duration when one is named.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveSlice>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveImage>(),
        Step.FromType<TakeSnapshot>()
    ];

    public override JobKind Kind => JobKind.Work;

    protected override void ValidateSettings(SettingsValidator<TSettings> settings) => settings
        .Require(s => s.Backup is { Paths.Count: > 0 }, "'backup.paths' names nothing to snapshot.")
        .Require(s => s.Backup?.Stop is null or { Length: > 0 }, "'backup.stop' must name a container, or be left out for a warm snapshot.");

    protected override void Configure(IWorkflowBuilder builder, TSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker().AddRestic();
        builder.Services.AddSingleton(new RequiredVolumes([.. settings.Volumes.Select(v => v.Directory)]));
        builder.Services.AddSingleton(new BackupPlan(
            [.. settings.Backup!.Paths.Select(p => p.Directory)],
            [.. settings.Backup.Excludes.Select(p => p.Value)],
            settings.Backup.Stop,
            settings.Backup.Image ?? settings.Backup.Stop));
    }
}
