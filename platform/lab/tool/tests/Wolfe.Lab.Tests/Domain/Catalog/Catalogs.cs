using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Tests.Domain.Catalog;

/// <summary>
/// Catalogs for a test, made as the reader makes them — each service and component created, then
/// added to the catalog or its service — from what a test says each component declares.
/// </summary>
internal static class Catalogs
{
    /// <summary>
    /// What a test says a component declares; a compose service makes it a compose component, a
    /// placement and an agent an agent component.
    /// </summary>
    internal sealed record Declaration(
        string Name,
        ComponentKind Kind,
        WorkflowName Workflow,
        string? PartOf = null,
        string[]? DependsOn = null,
        string? ComposeService = null,
        LogTransport? Logs = null,
        MetricsEndpoint? Metrics = null,
        DeploymentTarget? RunsOn = null,
        AgentProcess? Agent = null);

    /// <summary>
    /// An agent component <paramref name="name"/>, running <paramref name="agent"/> on the nodes of <paramref name="runsOn"/>.
    /// </summary>
    public static Declaration Agent(string name, DeploymentTarget runsOn, AgentProcess agent) =>
        new(name, ComponentKind.Collector, WorkflowName.Agents, RunsOn: runsOn, Agent: agent);

    /// <summary>
    /// A node as <c>platform/nodes.yaml</c> would declare it, carrying the runner of its name
    /// capitalised: <c>mini</c> runs <c>Mini</c>.
    /// </summary>
    public static Node Node(string name, NodeRole role, NodePlatform? platform = null, string? docker = null, params string[] drives)
    {
        var node = Wolfe.Lab.Domain.Catalog.Nodes.Node.Create(new DocumentSource(RepositoryPath.From("platform/nodes.yaml")), NodeName.From(name), role,
            platform ?? NodePlatform.DarwinArm64, Wolfe.Lab.Domain.Network.HostName.From($"{name}.tailnet.ts.net"), char.ToUpperInvariant(name[0]) + name[1..]).Value.ShouldNotBeNull();
        node.Docker = docker is null ? null : Wolfe.Lab.Domain.Network.DockerHost.From(docker);
        node.Drives = [.. drives.Select(Wolfe.Lab.Domain.Paths.HostPath.From)];
        return node;
    }

    /// <summary>
    /// A catalog of <paramref name="nodes"/> alone, for <see cref="Of(ServiceCatalog, string, Declaration[])"/> to add to.
    /// </summary>
    public static ServiceCatalog Nodes(params Node[] nodes)
    {
        var catalog = new ServiceCatalog();
        foreach (var node in nodes)
        {
            catalog.Add(node).Value.ShouldNotBeNull();
        }

        return catalog;
    }

    /// <summary>
    /// A component of a workflow that reads nothing beyond what every one does.
    /// </summary>
    public static Declaration Definition(string name, ComponentKind kind, WorkflowName workflow, string? partOf = null) =>
        new(name, kind, workflow, partOf);

    /// <summary>
    /// A component <paramref name="name"/> that runs as the compose service <paramref name="service"/>.
    /// </summary>
    public static Declaration Compose(string name, string service, ComponentKind? kind = null, LogTransport? logs = null, MetricsEndpoint? metrics = null) =>
        new(name, kind ?? ComponentKind.App, WorkflowName.Docker, ComposeService: service, Logs: logs, Metrics: metrics);

    /// <summary>
    /// Adds <paramref name="declaration"/> to <paramref name="service"/>, declared at <paramref name="source"/>.
    /// </summary>
    public static Result<Component> Add(Service service, DocumentSource source, Declaration declaration)
    {
        var name = ComponentName.From(declaration.Name);
        var partOf = declaration.PartOf is { } whole ? ComponentName.From(whole) : (ComponentName?)null;
        IReadOnlyList<ComponentName> dependsOn = [.. (declaration.DependsOn ?? []).Select(ComponentName.From)];
        if (declaration is { RunsOn: { } runsOn, Agent: { } agent })
        {
            var placed = AgentComponent.Create(source, name, declaration.Kind, declaration.Workflow, partOf, dependsOn, runsOn, agent);
            return placed.Value is { } agentComponent ? service.Add<Component>(agentComponent) : new Result<Component>(placed.Errors ?? []);
        }

        var created = declaration.ComposeService is { } composeService
            ? Created(ComposeComponent.Create(source, name, declaration.Kind, declaration.Workflow, partOf, dependsOn, ComposeServiceName.From(composeService)),
                declaration)
            : Wolfe.Lab.Domain.Catalog.Components.Component.Create(source, name, declaration.Kind, declaration.Workflow, partOf, dependsOn);
        return created.Value is { } component ? service.Add(component) : created;
    }

    /// <summary>
    /// Adds a service named for its directory, <paramref name="directory"/>, declared in its <c>service.yaml</c>.
    /// </summary>
    public static Result<Service> AddService(ServiceCatalog catalog, string directory, params string[] dependsOn)
    {
        var service = Service.Create(new DocumentSource(RepositoryPath.From(directory).Combine("service.yaml")), ServiceName.From(RepositoryPath.From(directory).Segments[^1]),
            [.. dependsOn.Select(ServiceName.From)]);
        return service.Value is { } created ? catalog.Add(created) : service;
    }

    // A compose component made, with its logs and metrics as the test says, as a component: a
    // Result of the derived type is not one of its base.
    private static Result<Component> Created(Result<ComposeComponent> made, Declaration declaration)
    {
        if (made.Value is not { } component)
        {
            return new Result<Component>(made.Errors ?? []);
        }

        component.Logs = declaration.Logs;
        component.Metrics = declaration.Metrics;
        return component;
    }

    /// <summary>
    /// The catalog of the components <paramref name="directory"/> declares, documents of one file
    /// in it, with their service declared.
    /// </summary>
    public static ServiceCatalog Of(string directory, params Declaration[] components) => Of(new ServiceCatalog(), directory, components);

    /// <summary>
    /// <paramref name="catalog"/>, with the components <paramref name="directory"/> declares added
    /// to it, as <see cref="Of(string, Declaration[])"/> adds them.
    /// </summary>
    public static ServiceCatalog Of(ServiceCatalog catalog, string directory, params Declaration[] components)
    {
        var at = RepositoryPath.From(directory);
        var service = AddService(catalog, (at.Parent ?? throw new ArgumentException($"{directory} has no service.")).Value).Value.ShouldNotBeNull();
        for (var index = 0; index < components.Length; index++)
        {
            Add(service, new DocumentSource(at.Combine("component.yaml"), index, components.Length), components[index]).Value.ShouldNotBeNull();
        }

        return catalog;
    }

    /// <summary>
    /// What <paramref name="directory"/> declares, as a deploy finds it.
    /// </summary>
    public static DeploymentUnit Unit(string directory, params Declaration[] components) =>
        Of(directory, components).DeploymentUnitAt(RepositoryPath.From(directory)).ShouldNotBeNull();

    /// <summary>
    /// The one component <paramref name="directory"/> declares.
    /// </summary>
    public static Component Component(string directory, Declaration declaration) =>
        Unit(directory, declaration).Components.ShouldHaveSingleItem();

    /// <summary>
    /// The one component <paramref name="directory"/> declares: <paramref name="name"/>, run as the
    /// compose service of the same name.
    /// </summary>
    public static Component Component(string directory, string name) => Component(directory, Compose(name, name));
}
