using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Workflows.Docker.Steps;

namespace Wolfe.Lab.Application.Workflows.Docker.Jobs;

/// <summary>
/// Proves an image component could be built and pushed.
/// </summary>
internal sealed class ImageCheckJob : LabJob<DeclaredSettings>
{
    public override string Name => "check";

    public override string Description => "Checks the component's image has a Dockerfile; its tag names its registry.";

    public override IReadOnlyList<Step> Steps { get; } = [
        Step.FromType<GatePathFilter>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<CheckImages>()
    ];

    public override JobKind Kind => JobKind.Check;
}
