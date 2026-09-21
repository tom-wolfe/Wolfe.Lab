using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Backup.Services;
using Wolfe.Lab.Build.Backup.Steps;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Restic;
using Wolfe.Lab.Build.Steps;

namespace Wolfe.Lab.Build.Backup.Jobs;

/// <summary>
/// Brings a slice's state back from a snapshot: the restore every runbook describes, as one
/// command with the safety rails built in.
/// </summary>
/// <typeparam name="TSettings">The slice's <c>ritten.json</c> shape, which carries the <c>backup</c> section.</typeparam>
internal sealed class RestoreJob<TSettings> : LabJob<TSettings> where TSettings : SliceSettings
{
    internal static readonly JobArgument<string> Snapshot = JobArgument.Value<string>(
        "snapshot",
        "The snapshot to restore, by id. The slice's latest when left out.");

    internal static readonly JobArgument<bool> AnyImage = JobArgument.Value<bool>(
        "any-image",
        "Restore onto whatever image the stack runs, rather than the one the snapshot was taken under.");

    public override string Name => "restore";

    public override string Description => "Stops the stack, sets its state aside, restores a snapshot in its place and starts it again.";

    public override IReadOnlyList<JobArgument> Arguments { get; } = [Snapshot, AnyImage];

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveSlice>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveSnapshot>(),
        Step.FromType<ResolveImage>(),
        Step.FromType<CheckImage>(),
        Step.FromType<GateApproval>(),
        Step.FromType<RestoreState>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<TSettings> settings) => settings
        .Require(s => s.Backup.Paths.Count > 0, "'backup.paths' names nothing to restore.");

    protected override void Configure(IWorkflowBuilder builder, TSettings settings, JobArguments args)
    {
        base.Configure(builder, settings, args);
        builder.AddDocker().AddRestic().AddStateDirectories();
        builder.Services.AddSingleton(new RequiredVolumes([.. settings.Volumes.Select(v => v.Directory)]));
        builder.Services.AddSingleton(settings.Backup.ToPlan());
        builder.Services.AddSingleton(new RestoreRequest(args.Get(Snapshot), args.Get(AnyImage)));
    }
}
