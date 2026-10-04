using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Represents all the services available through the lab.
/// </summary>
public sealed class ServiceCatalog
{
    private readonly List<Service> _services = [];

    /// <summary>
    /// Every service, by area and name.
    /// </summary>
    public IReadOnlyList<Service> Services => _services;

    /// <summary>
    /// Adds a service to the catalog.
    /// </summary>
    public Result<Service> Add(Service service)
    {
        var problems = new List<Error>();
        if (_services.FirstOrDefault(other => other.Directory == service.Directory) is { } there)
        {
            problems.Add(ServiceErrors.DeclaredAlready(service.Name.Value, there.Source));
        }
        else if (FindService(service.Name) is { } namesake)
        {
            problems.Add(ServiceErrors.NameShared(service.Name, namesake.Source));
        }

        problems.AddRange(service.DependsOn.Where(needed => FindService(needed) is null).Select(ServiceErrors.DependsOnUndeclared));

        if (problems.Count > 0)
        {
            return problems.Select(Error (problem) => CatalogError.In(service.Source, problem)).ToList();
        }

        var position = _services.FindIndex(other => string.CompareOrdinal(other.Directory.Value, service.Directory.Value) > 0);
        _services.Insert(position < 0 ? _services.Count : position, service);
        return service;
    }

    /// <summary>
    /// Its service named <paramref name="name"/>, if it has one.
    /// </summary>
    public Service? FindService(ServiceName name) => _services.FirstOrDefault(service => service.Name == name);

    /// <summary>
    /// The service a component declared at <paramref name="source"/> belongs to.
    /// </summary>
    public Result<Service> ServiceDeclaring(DocumentSource source)
    {
        var owner = source.File.Directories switch
        {
            [var area, var service, ..] and ([_, _] or [_, _, _]) => RepositoryPath.From($"{area}/{service}"),
            _ => (RepositoryPath?)null
        };
        if (owner is not { } directory)
        {
            return CatalogError.In(source, ComponentErrors.OutOfPlace);
        }

        return _services.FirstOrDefault(service => service.Directory == directory) is { } found
            ? found
            : CatalogError.In(source, ComponentErrors.NoService(directory));
    }

    /// <summary>
    /// What <paramref name="directory"/> declares, deployed together; null when it declares nothing.
    /// </summary>
    public DeploymentUnit? DeploymentUnitAt(RepositoryPath directory) =>
        _services.FirstOrDefault(service => service.Components.Any(component => component.Directory == directory)) is { } owner
            ? new DeploymentUnit(owner, directory, [.. owner.Components.Where(component => component.Directory == directory)])
            : null;
}
