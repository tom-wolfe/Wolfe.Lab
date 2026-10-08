using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;

namespace Wolfe.Lab.Domain.Catalog.Components.Compose;

/// <summary>
/// A component the <c>docker</c> workflow operates: one service of the compose stack in its
/// directory.
/// </summary>
public class DockerComponent : Component
{
    private protected DockerComponent() { }

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
    /// Creates a new Docker component.
    /// </summary>
    public static Result<DockerComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, ComposeServiceName composeService)
    {
        var errors = Validate(source, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new DockerComponent
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = WorkflowName.Docker,
            ComposeService = composeService
        };
    }
}
