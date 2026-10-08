using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Releases;

namespace Wolfe.Lab.Application.Workflows.Ollama.Jobs;

/// <summary>
/// Checks the models' declarations: the catalog holds every rule between them and their servers.
/// </summary>
internal sealed class CheckJob : LabJob<DeclaredSettings>
{
    public override string Name => "check";

    public override string Description => "Checks the models each server serves, by their use.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>()
    ];

    public override JobKind Kind => JobKind.Check;
}
