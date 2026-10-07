namespace Wolfe.Lab.Domain.Catalog.Components.Compose;

/// <summary>
/// A component the <c>dotnet-service</c> workflow operates: a service of the compose stack in its
/// directory, run from an image the lab builds out of that directory's source.
/// </summary>
public sealed class DotNetServiceComponent : DockerComponent
{
    private DotNetServiceComponent() { }

    /// <summary>
    /// The image the lab builds it from: <c>lab/&lt;service&gt;-&lt;component&gt;</c>.
    /// </summary>
    public string Image => $"lab/{Service.Name}-{Name}";

    /// <summary>
    /// Creates a new .NET service component.
    /// </summary>
    public static new Result<DotNetServiceComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, ComponentName? partOf, IReadOnlyList<ComponentName> dependsOn, ComposeServiceName composeService)
    {
        var errors = Validate(source, name, partOf, dependsOn, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new DotNetServiceComponent
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = WorkflowName.DotNetService,
            PartOf = partOf,
            DependsOn = dependsOn,
            ComposeService = composeService
        };
    }
}
