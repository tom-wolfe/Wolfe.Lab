using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Represents the components of a service that are deployed together.
/// </summary>
public sealed class DeploymentUnit
{
    private DeploymentUnit(Service service, RepositoryPath directory, IReadOnlyList<Component> components, Component head)
    {
        Service = service;
        Directory = directory;
        Components = components;
        Head = head;
    }

    /// <summary>
    /// The service they all belong to.
    /// </summary>
    public Service Service { get; }

    /// <summary>
    /// The directory they are declared in.
    /// </summary>
    public RepositoryPath Directory { get; }

    /// <summary>
    /// Each component it declares, by name.
    /// </summary>
    public IReadOnlyList<Component> Components { get; }

    /// <summary>
    /// Gets the main component. The one with no dependents.
    /// </summary>
    public Component Head { get; }

    /// <summary>
    /// What name of the unit, made from its service name and head component.
    /// </summary>
    public string Name => $"{Service.Name}-{Head.Name}";

    /// <summary>
    /// The volumes its components require.
    /// </summary>
    public IReadOnlyList<HostPath> RequiresVolumes => [.. Components.SelectMany(component => component.RequiresVolumes).Distinct()];

    /// <summary>
    /// The <paramref name="components"/> of <paramref name="service"/> declared in
    /// <paramref name="directory"/>, when exactly one of them is at their head.
    /// </summary>
    internal static Result<DeploymentUnit> Create(Service service, RepositoryPath directory, IReadOnlyList<Component> components)
    {
        var names = components.Select(component => component.Name).ToHashSet();
        var needed = components.SelectMany(component => component.DependsOn).ToHashSet();
        var heads = components.Where(component => !needed.Contains(component.Name) && !(component.PartOf is { } whole && names.Contains(whole))).ToList();
        return heads is [var head]
            ? new DeploymentUnit(service, directory, components, head)
            : CatalogError.In(new DocumentSource(components[0].Source.File), DeploymentUnitErrors.NotOneHead(directory, [.. heads.Select(component => component.Name)]));
    }

    /// <summary>
    /// Its one component <paramref name="workflow"/> operates, or why there is not exactly one.
    /// </summary>
    public Result<Component> ByWorkflow(WorkflowName workflow) =>
        Components.Where(component => component.Workflow == workflow).ToList() is [var only]
            ? only
            : DeploymentUnitErrors.NotOneOf(Directory, workflow, Components.Count(component => component.Workflow == workflow));

    /// <inheritdoc />
    public override string ToString() => Directory.Value;
}
