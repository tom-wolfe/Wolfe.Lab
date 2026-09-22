using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Backup.Steps;
using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Slices;
using Wolfe.Lab.Build.Slices.Steps;

namespace Wolfe.Lab.Build.Backup;

/// <summary>
/// Proves the slice's backup can be restored from.
/// </summary>
/// <typeparam name="TSettings">The slice's <c>ritten.json</c> shape, which carries the <c>backup</c> section.</typeparam>
internal sealed class DrillJob<TSettings> : LabJob<TSettings> where TSettings : SliceSettings, IBackupSettings
{
    public override string Name => "restore-drill";

    public override string Description => "Restores what proves the slice's latest snapshot into a scratch directory and asserts it came back.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveSlice>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<DrillRestore>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<TSettings> settings) => settings
        .Require(s => s.Backup.Paths.Count > 0, "'backup.paths' names nothing to snapshot.")
        .Require(s => s.Backup.Verify.Count > 0, "'backup.verify' names nothing that proves a restore.");

    protected override void Configure(IWorkflowBuilder builder, TSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddRestic();
        builder.Services.AddSingleton(new RequiredVolumes([.. settings.Volumes.Select(v => v.Directory)]));
        builder.Services.AddSingleton(settings.Backup.ToPlan());
    }
}
