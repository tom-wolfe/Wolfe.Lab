using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Represents the components of a service that are deployed together.
/// </summary>
public sealed class DeploymentUnit
{
    internal DeploymentUnit(Service service, RepositoryPath directory, IReadOnlyList<Component> components)
    {
        Service = service;
        Directory = directory;
        Components = components;
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
    /// Its one component <paramref name="workflow"/> operates, or why there is not exactly one.
    /// </summary>
    public Result<Component> ByWorkflow(WorkflowName workflow) =>
        Components.Where(component => component.Workflow == workflow).ToList() is [var only]
            ? only
            : DeploymentUnitErrors.NotOneOf(Directory, workflow, Components.Count(component => component.Workflow == workflow));

    /// <inheritdoc />
    public override string ToString() => Directory.Value;
}
