using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog.Services;

/// <summary>
/// Represents a service that runs in the lab.
/// </summary>
public sealed class Service : IEquatable<Service>
{
    private readonly List<Component> _components = [];
    private ServiceCatalog? _catalog;

    private Service() { }

    /// <summary>
    /// The catalog it is listed in.
    /// </summary>
    public ServiceCatalog Catalog => _catalog ?? throw new InvalidOperationException($"{Name} is in no catalog until one adds it.");

    /// <summary>
    /// The area it is classified under: its directory's parent.
    /// </summary>
    public required AreaName Area { get; init; }

    /// <summary>
    /// Its name, which is its directory's, and the lab's alone.
    /// </summary>
    public required ServiceName Name { get; init; }

    /// <summary>
    /// The name people read, when the name is not it.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// What it is, in a sentence.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Where it is in its life.
    /// </summary>
    public Lifecycle Lifecycle { get; set; } = Lifecycle.Production;

    /// <summary>
    /// Where to read about it, use it and watch it.
    /// </summary>
    public IReadOnlyList<ServiceLink> Links { get; set; } = [];

    /// <summary>
    /// The services it needs to run, by name (<see cref="ServiceCatalog.FindService"/>).
    /// </summary>
    public IReadOnlyList<ServiceName> DependsOn { get; init; } = [];

    /// <summary>
    /// Where it is declared.
    /// </summary>
    public required DocumentSource Source { get; init; }

    /// <summary>
    /// Its directory in the repository: <c>area/service</c>.
    /// </summary>
    public RepositoryPath Directory => RepositoryPath.From($"{Area}/{Name}");

    /// <summary>
    /// Its components, by name.
    /// </summary>
    public IReadOnlyList<Component> Components => _components;

    /// <summary>
    /// Creates a new service.
    /// </summary>
    public static Result<Service> Create(DocumentSource source, ServiceName name, IReadOnlyList<ServiceName> dependsOn)
    {
        if (source.Directories is not [var area, var directory])
        {
            return CatalogError.In(source, ServiceErrors.NotInItsOwnDirectory);
        }

        if (AreaName.TryFrom(area) is not { IsSuccess: true } named)
        {
            return CatalogError.In(source, ServiceErrors.AreaNotAName(area));
        }

        var problems = new List<Error>();
        if (name.Value != directory)
        {
            problems.Add(ServiceErrors.NamedOtherThanItsDirectory(name, directory));
        }

        if (dependsOn.Contains(name))
        {
            problems.Add(ServiceErrors.DependsOnItself(name));
        }

        return problems.Count > 0
            ? problems.Select(Error (problem) => CatalogError.In(source, problem)).ToList()
            : new Service { Area = named.ValueObject, Name = name, DependsOn = dependsOn, Source = source };
    }

    /// <summary>
    /// Attempts to add the component to the catalog, and returns the result.
    /// </summary>
    public Result<T> Add<T>(T component) where T : Component
    {
        if (component.Directory != Directory && component.Directory.Parent != Directory)
        {
            return CatalogError.In(component.Source, ComponentErrors.OutOfPlace);
        }

        var problems = new List<Error>();
        if (FindComponent(component.Name) is { } taken)
        {
            problems.Add(ComponentErrors.DeclaredAlready(component.Name, taken.Source));
        }

        if (component.PartOf == component.Name)
        {
            problems.Add(ComponentErrors.PartOfItself(component.Name));
        }
        else if (component.PartOf is { } whole && FindComponent(whole) is null)
        {
            problems.Add(ComponentErrors.PartOfUndeclared(whole));
        }

        if (component.DependsOn.Contains(component.Name))
        {
            problems.Add(ComponentErrors.DependsOnItself(component.Name));
        }

        problems.AddRange(component.DependsOn.Where(needed => needed != component.Name && FindComponent(needed) is null).Select(ComponentErrors.DependsOnUndeclared));
        if (problems.Count == 0)
        {
            problems.AddRange(component.SetService(this));
        }

        if (problems.Count > 0)
        {
            return problems.Select(Error (problem) => CatalogError.In(component.Source, problem)).ToList();
        }
        var at = _components.FindIndex(sibling => string.CompareOrdinal(sibling.Name.Value, component.Name.Value) > 0);
        _components.Insert(at < 0 ? _components.Count : at, component);
        return component;
    }

    /// <summary>
    /// Makes it <paramref name="catalog"/>'s.
    /// </summary>
    internal void Join(ServiceCatalog catalog)
    {
        if (_catalog is not null)
        {
            throw new InvalidOperationException($"{this} is a catalog's already; a service is listed once.");
        }

        _catalog = catalog;
    }

    /// <summary>
    /// Its component named <paramref name="name"/>, if it has one.
    /// </summary>
    public Component? FindComponent(ComponentName name) => _components.FirstOrDefault(component => component.Name == name);

    /// <inheritdoc />
    public bool Equals(Service? other) => other is not null && Name == other.Name;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Service);

    /// <inheritdoc />
    public override int GetHashCode() => Name.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"{Area}/{Name}";
}
