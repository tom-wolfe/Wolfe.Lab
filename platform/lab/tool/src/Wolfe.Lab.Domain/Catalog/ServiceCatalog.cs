using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Represents all the services available through the lab.
/// </summary>
public sealed class ServiceCatalog
{
    private readonly List<Service> _services = [];
    private readonly List<Node> _nodes = [];

    /// <summary>
    /// Every service, by area and name.
    /// </summary>
    public IReadOnlyList<Service> Services => _services;

    /// <summary>
    /// The machines the lab runs on, by name.
    /// </summary>
    public IReadOnlyList<Node> Nodes => _nodes;

    /// <summary>
    /// Adds a node to the catalog.
    /// </summary>
    public Result<Node> Add(Node node)
    {
        var problems = new List<Error>();
        if (FindNode(node.Name) is { } namesake)
        {
            problems.Add(NodeErrors.DeclaredAlready(node.Name, namesake.Source));
        }

        if (NodeRunning(node.Runner) is { } other)
        {
            problems.Add(NodeErrors.RunnerShared(node.Runner, other.Source));
        }

        if (problems.Count > 0)
        {
            return problems.Select(Error (problem) => CatalogError.In(node.Source, problem)).ToList();
        }

        var position = _nodes.FindIndex(other => string.CompareOrdinal(other.Name.Value, node.Name.Value) > 0);
        _nodes.Insert(position < 0 ? _nodes.Count : position, node);
        return node;
    }

    /// <summary>
    /// Its node named <paramref name="name"/>, if it has one.
    /// </summary>
    public Node? FindNode(NodeName name) => _nodes.FirstOrDefault(node => node.Name == name);

    /// <summary>
    /// The node that carries the runner <paramref name="runner"/>, if one does.
    /// </summary>
    public Node? NodeRunning(string runner) => _nodes.FirstOrDefault(node => node.Runner == runner);

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

        service.Join(this);
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
