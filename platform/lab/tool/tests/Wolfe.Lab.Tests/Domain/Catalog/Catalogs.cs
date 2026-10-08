using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Components.Backups;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Domain.Catalog.Components.Models;
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
    /// What a test says a component declares; a compose service makes it a Docker component, a
    /// placement and an agent an agents component.
    /// </summary>
    internal sealed record Declaration(
        string Name,
        ComponentKind Kind,
        WorkflowName Workflow,
        string? PartOf = null,
        string[]? DependsOn = null,
        string? ComposeService = null,
        LogTransport? Logs = null,
        IReadOnlyList<MetricsEndpoint>? Metrics = null,
        DeploymentTarget? RunsOn = null,
        AgentProcess? Agent = null,
        Snapshot? Backup = null,
        Use? Serving = null);

    /// <summary>
    /// What a test says a model serves, makes a declaration a model component: its defaults, and
    /// each server with what it runs otherwise.
    /// </summary>
    internal sealed record Use(string? Model, int? Context, (string Server, string? Model, int? Context)[] ServedBy);

    /// <summary>
    /// A model used for <paramref name="name"/>, running <paramref name="model"/> on each of <paramref name="servers"/>.
    /// </summary>
    public static Declaration Model(string name, string model, params string[] servers) =>
        new(name, ComponentKind.Model, WorkflowName.Ollama, Serving: new Use(model, null, [.. servers.Select(server => (server, (string?)null, (int?)null))]));

    /// <summary>
    /// What a test says a backup snapshots, makes a declaration a backup component.
    /// </summary>
    internal sealed record Snapshot(string[] Paths, string[]? Excludes = null, string[]? Verify = null, bool Warm = false);

    /// <summary>
    /// A backup <paramref name="name"/> of <paramref name="paths"/>, part of <paramref name="partOf"/> when it is part of anything.
    /// </summary>
    public static Declaration Backup(string name, string? partOf, params string[] paths) =>
        new(name, ComponentKind.Backup, WorkflowName.Backup, partOf, Backup: new Snapshot(paths));

    /// <summary>
    /// <paramref name="values"/>, as the templates an agent's fields are.
    /// </summary>
    public static IReadOnlyList<Template> Templates(params string[] values) => [.. values.Select(Template.From)];

    /// <summary>
    /// An agent's environment of <paramref name="variables"/>, each value a template.
    /// </summary>
    public static IReadOnlyDictionary<string, Template> Variables(params (string Name, string Value)[] variables) =>
        variables.ToDictionary(variable => variable.Name, variable => Template.From(variable.Value), StringComparer.Ordinal);

    /// <summary>
    /// What <paramref name="templates"/> say once written out, for comparing with what a test expects.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Written(IReadOnlyDictionary<string, Template> templates) =>
        templates.ToDictionary(template => template.Key, template => template.Value.Value, StringComparer.Ordinal);

    /// <summary>
    /// An agent component <paramref name="name"/>, running <paramref name="agent"/> on the nodes of <paramref name="runsOn"/>.
    /// </summary>
    public static Declaration Agent(string name, DeploymentTarget runsOn, AgentProcess agent) =>
        new(name, ComponentKind.Collector, WorkflowName.Agent, RunsOn: runsOn, Agent: agent);

    /// <summary>
    /// A node as <c>platform/nodes.yaml</c> would declare it, keeping the lab in <c>/lab/root</c>
    /// and <c>/lab/data</c>.
    /// </summary>
    public static Node Node(string name, NodeRole role, NodePlatform? platform = null, string? docker = null, params string[] drives)
    {
        var node = Wolfe.Lab.Domain.Catalog.Nodes.Node.Create(new DocumentSource(RepositoryPath.From("platform/nodes.yaml")), NodeName.From(name), role,
            platform ?? NodePlatform.DarwinArm64, Wolfe.Lab.Domain.Network.HostName.From($"{name}.tailnet.ts.net"),
            new NodeDirectories(AbsolutePath.From("/lab/root"), AbsolutePath.From("/lab/data"))).Value.ShouldNotBeNull();
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
    public static Declaration Docker(string name, string service, ComponentKind? kind = null, LogTransport? logs = null, params MetricsEndpoint[] metrics) =>
        new(name, kind ?? ComponentKind.App, WorkflowName.Docker, ComposeService: service, Logs: logs, Metrics: metrics);

    /// <summary>
    /// Adds <paramref name="declaration"/> to <paramref name="service"/>, declared at <paramref name="source"/>.
    /// </summary>
    public static Result<Component> Add(Service service, DocumentSource source, Declaration declaration)
    {
        var name = ComponentName.From(declaration.Name);
        var created = declaration switch
        {
            { Serving: { } use } => Made(ModelComponent.Create(source, name, declaration.Kind,
                use.ServedBy.ToDictionary(server => ComponentName.From(server.Server),
                    server => new ModelServing(server.Model is { } own ? ModelTag.From(own) : null, server.Context is { } tokens ? ContextLength.From(tokens) : null))), model =>
            {
                model.Model = use.Model is { } runs ? ModelTag.From(runs) : null;
                model.Context = use.Context is { } tokens ? ContextLength.From(tokens) : null;
            }),
            { Backup: { } snapshot } => Made(BackupComponent.Create(source, name, declaration.Kind, [.. snapshot.Paths.Select(HostPath.From)]), backup =>
            {
                backup.Excludes = [.. (snapshot.Excludes ?? []).Select(HostPath.From)];
                backup.Verify = [.. (snapshot.Verify ?? []).Select(HostPath.From)];
                backup.Warm = snapshot.Warm;
            }),
            { RunsOn: { } runsOn, Agent: { } agent } => Made(AgentComponent.Create(source, name, declaration.Kind, declaration.Workflow, runsOn, agent), _ => { }),
            { ComposeService: { } built } when declaration.Workflow == WorkflowName.DotNetService =>
                Made(DotNetServiceComponent.Create(source, name, declaration.Kind, ComposeServiceName.From(built)), docker => Reports(docker, declaration)),
            { ComposeService: { } composeService } =>
                Made(DockerComponent.Create(source, name, declaration.Kind, ComposeServiceName.From(composeService)), docker => Reports(docker, declaration)),
            _ => Wolfe.Lab.Domain.Catalog.Components.Component.Create(source, name, declaration.Kind, declaration.Workflow)
        };

        if (created.Value is not { } component)
        {
            return created;
        }

        component.PartOf = declaration.PartOf is { } whole ? ComponentName.From(whole) : null;
        component.DependsOn = [.. (declaration.DependsOn ?? []).Select(ComponentName.From)];
        return service.Add(component);
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

    // A component made and given what the test declares beyond what it is made with, as a
    // component: a Result of the derived type is not one of its base.
    private static Result<Component> Made<T>(Result<T> made, Action<T> declares) where T : Component
    {
        if (made.Value is not { } component)
        {
            return new Result<Component>(made.Errors ?? []);
        }

        declares(component);
        return component;
    }

    // A Docker component's logs and metrics, as the test says.
    private static void Reports(DockerComponent component, Declaration declaration)
    {
        component.Logs = declaration.Logs;
        component.Metrics = declaration.Metrics ?? [];
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
        Of(directory, components).DeploymentUnitAt(RepositoryPath.From(directory)).ShouldNotBeNull().Value.ShouldNotBeNull();

    /// <summary>
    /// The one component <paramref name="directory"/> declares.
    /// </summary>
    public static Component Component(string directory, Declaration declaration) =>
        Unit(directory, declaration).Components.ShouldHaveSingleItem();

    /// <summary>
    /// The one component <paramref name="directory"/> declares: <paramref name="name"/>, run as the
    /// compose service of the same name.
    /// </summary>
    public static Component Component(string directory, string name) => Component(directory, Docker(name, name));
}
