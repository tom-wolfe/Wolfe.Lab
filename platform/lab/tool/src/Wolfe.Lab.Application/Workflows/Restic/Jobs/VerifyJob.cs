using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Restic;
using Wolfe.Lab.Application.Volumes;
using Wolfe.Lab.Application.Workflows.Restic.Steps;

namespace Wolfe.Lab.Application.Workflows.Restic.Jobs;

/// <summary>
/// The weekly integrity check of both repositories.
/// </summary>
internal sealed class VerifyJob : ResticJob
{
    public override string Name => "verify";

    public override string Description => "Checks both repositories' integrity, reading a sample of the offsite copy's data back.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveRepositories>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveRepository>(),
        Step.FromType<ResolveOffsite>(),
        Step.FromType<CheckRepositories>()
    ];

    public override JobKind Kind => JobKind.Check;



}
