using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;

namespace Wolfe.Lab.Domain.Catalog.Components.Compose;

/// <summary>
/// A component a compose workflow (<c>docker</c>, <c>dotnet-service</c>) operates: one service of
/// the compose stack in its directory.
/// </summary>
public sealed class ComposeComponent : Component
{
    private ComposeComponent() { }

    /// <summary>
    /// The compose service it converges: the container keeps Docker's unique name on the node,
    /// the component the catalog's.
    /// </summary>
    public required ComposeServiceName ComposeService { get; init; }

    /// <summary>
    /// How its logs reach the lab, when not as its output.
    /// </summary>
    public LogTransport? Logs { get; set; }

    /// <summary>
    /// Where it serves its metrics, if it does.
    /// </summary>
    public IReadOnlyList<MetricsEndpoint> Metrics { get; set; } = [];

    /// <summary>
    /// Creates a new compose component.
    /// </summary>
    public static Result<ComposeComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, WorkflowName workflow, ComponentName? partOf, IReadOnlyList<ComponentName> dependsOn, ComposeServiceName composeService)
    {
        var errors = Validate(source, name, partOf, dependsOn, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new ComposeComponent
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = workflow,
            PartOf = partOf,
            DependsOn = dependsOn,
            ComposeService = composeService
        };
    }
}
