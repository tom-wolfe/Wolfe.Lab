using Wolfe.Lab.Domain.Catalog.Components.Compose;

namespace Wolfe.Lab.Domain.Catalog.Components.Garage;

/// <summary>
/// A component the <c>garage</c> workflow operates: Garage's service of its compose stack, and the
/// role its node takes in the cluster's layout.
/// </summary>
public sealed class GarageComponent : DockerComponent
{
    private GarageComponent() { }

    /// <summary>
    /// The node's role, brought into line on every deploy.
    /// </summary>
    public required GarageLayout Layout { get; init; }

    /// <summary>
    /// Creates a new Garage component.
    /// </summary>
    public static Result<GarageComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, ComposeServiceName composeService, GarageLayout layout)
    {
        var errors = Validate(source, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new GarageComponent
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = WorkflowName.Garage,
            ComposeService = composeService,
            Layout = layout
        };
    }
}
