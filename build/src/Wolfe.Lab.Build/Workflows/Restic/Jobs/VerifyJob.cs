using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Restic.Steps;
using Wolfe.Lab.Build.Clients.Volumes.Steps;
using Wolfe.Lab.Build.Workflows.Restic.Models;
using Wolfe.Lab.Build.Workflows.Restic.Steps;

namespace Wolfe.Lab.Build.Workflows.Restic.Jobs;

/// <summary>
/// The weekly integrity check of both repositories.
/// </summary>
internal sealed partial class VerifyJob : ResticJob
{
    public override string Name => "verify";

    public override string Description => "Checks both repositories' integrity, reading a sample of the offsite copy's data back.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveOffsite>(),
        Step.FromType<CheckRepositories>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<ResticSettings> settings) => settings
        .Require(s => Subset().IsMatch(s.Verify.ReadDataSubset), "'verify.readDataSubset' must be a percentage the way restic spells it, such as 5%.");

    protected override void Configure(IWorkflowBuilder builder, ResticSettings settings)
    {
        base.Configure(builder, settings);
        builder.Services.AddSingleton(new VerifyOptions(settings.Verify.ReadDataSubset));
    }

    [GeneratedRegex(@"^(100|[1-9]?\d)%$")]
    private static partial Regex Subset();
}
