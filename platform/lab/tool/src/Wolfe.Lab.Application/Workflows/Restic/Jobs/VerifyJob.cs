using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Restic;
using Wolfe.Lab.Application.Volumes;
using Wolfe.Lab.Application.Workflows.Restic.Models;
using Wolfe.Lab.Application.Workflows.Restic.Steps;

namespace Wolfe.Lab.Application.Workflows.Restic.Jobs;

/// <summary>
/// The weekly integrity check of both repositories.
/// </summary>
internal sealed partial class VerifyJob : ResticJob
{
    public override string Name => "verify";

    public override string Description => "Checks both repositories' integrity, reading a sample of the offsite copy's data back.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveOffsite>(),
        Step.FromType<CheckRepositories>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<ResticOptions> options) => options
        .Require(s => Subset().IsMatch(s.Verify.ReadDataSubset), "'verify.readDataSubset' must be a percentage the way restic spells it, such as 5%.");

    protected override void Configure(IWorkflowBuilder builder, ResticOptions options)
    {
        base.Configure(builder, options);
        builder.Services.AddSingleton(options.Verify);
    }

    [GeneratedRegex(@"^(100|[1-9]?\d)%$")]
    private static partial Regex Subset();
}
