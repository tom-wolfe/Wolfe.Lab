using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Domain.Telemetry;

namespace Wolfe.Lab.Domain.Catalog.Components;

/// <summary>
/// A logical part of a service.
/// </summary>
public class Component : IEquatable<Component>
{
    private Service? _service;

    private protected Component() { }

    /// <summary>
    /// Creates a new component.
    /// </summary>
    public static Result<Component> Create(DocumentSource source, ComponentName name, ComponentKind kind, WorkflowName workflow, ComponentName? partOf, IReadOnlyList<ComponentName> dependsOn)
    {
        var errors = Validate(source, name, partOf, dependsOn, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new Component
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = workflow,
            PartOf = partOf,
            DependsOn = dependsOn
        };
    }

    private protected static List<Error> Validate(DocumentSource source, ComponentName name, ComponentName? partOf, IReadOnlyList<ComponentName> dependsOn, out RepositoryPath directory)
    {
        var problems = new List<Error>();
        var file = source.File;
        if (file.Directories is [_, _] or [_, _, _] && file.Parent is { } parent)
        {
            directory = parent;
        }
        else
        {
            directory = file;
            problems.Add(ComponentErrors.OutOfPlace);
        }

        if (partOf == name)
        {
            problems.Add(ComponentErrors.PartOfItself(name));
        }

        if (dependsOn.Contains(name))
        {
            problems.Add(ComponentErrors.DependsOnItself(name));
        }

        return [.. problems.Select(Error (problem) => CatalogError.In(source, problem))];
    }

    /// <summary>
    /// Attempts to set the owning service, and returns any errors.
    /// </summary>
    internal virtual IReadOnlyList<Error> SetService(Service service)
    {
        if (_service is not null)
        {
            throw new InvalidOperationException($"{this} is a service's already; a component is one service's, once.");
        }

        _service = service;
        return [];
    }

    /// <summary>
    /// The service it belongs to.
    /// </summary>
    public Service Service => _service ?? throw new InvalidOperationException($"{Name} is no service's until one adds it.");

    /// <summary>
    /// The area its service is classified under.
    /// </summary>
    public AreaName Area => Service.Area;

    /// <summary>
    /// The name of the component.
    /// </summary>
    public required ComponentName Name { get; init; }

    /// <summary>
    /// The name people read, when the name is not it.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// What it is, in a sentence.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// What its service uses it for.
    /// </summary>
    public required ComponentKind Kind { get; init; }

    /// <summary>
    /// The workflow that operates it.
    /// </summary>
    public required WorkflowName Workflow { get; init; }

    /// <summary>
    /// The components of its own service it needs, by name (<see cref="Services.Service.FindComponent"/>).
    /// </summary>
    public IReadOnlyList<ComponentName> DependsOn { get; init; } = [];

    /// <summary>
    /// Where it is declared.
    /// </summary>
    public required DocumentSource Source { get; init; }

    /// <summary>
    /// The directory the component is declared in.
    /// </summary>
    public required RepositoryPath Directory { get; init; }

    /// <summary>
    /// The sibling it lives inside, if any, by name (<see cref="Services.Service.FindComponent"/>).
    /// </summary>
    public ComponentName? PartOf { get; init; }

    /// <summary>
    /// Gets the first The nearest component (this one included), that the lab runs.
    /// </summary>
    public Component? Host => Workflow.IsHost ? this : PartOf is { } whole ? Service.FindComponent(whole)?.Host : null;

    /// <summary>
    /// Where the component lives, as the attributes its telemetry is labelled with.
    /// </summary>
    public IReadOnlyList<KeyValuePair<TelemetryAttribute, string>> Attributes =>
    [
        new(TelemetryAttribute.Area, Area.Value),
        new(TelemetryAttribute.Service, Service.Name.Value),
        new(TelemetryAttribute.Component, Name.Value)
    ];

    /// <summary>
    /// The attributes as OpenTelemetry's <c>OTEL_RESOURCE_ATTRIBUTES</c> spells them.
    /// </summary>
    public string ResourceAttributes => string.Join(',', Attributes.Select(attribute => $"{attribute.Key.Name}={attribute.Value}"));

    /// <summary>
    /// Where it lives as one name, for a file of the component's own: <c>ai-ollama-server</c>.
    /// </summary>
    public string QualifiedName => $"{Area}-{Service.Name}-{Name}";

    /// <inheritdoc />
    public bool Equals(Component? other) => other is not null && Equals(_service, other._service) && Name == other.Name;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Component);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_service, Name);

    /// <inheritdoc />
    public override string ToString() => _service is { } service ? $"{service}/{Name}" : Name.Value;
}
