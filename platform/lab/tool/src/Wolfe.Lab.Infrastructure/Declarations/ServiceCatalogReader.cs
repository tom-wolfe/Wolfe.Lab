using System.Text.Json;
using System.Text.Json.Nodes;
using Ritten.Git;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Network;
using Wolfe.Lab.Domain.Packages;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// Reads the service catalog from a Git repository.
/// </summary>
public static class ServiceCatalogReader
{
    private static readonly JsonSerializerOptions Serializer = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// The catalog the checkout at <paramref name="root"/> declares, or every problem with it.
    /// </summary>
    public static async Task<Result<ServiceCatalog>> Read(IGit git, IDirectory root, CancellationToken ct = default) =>
        Read(root, await DeclarationFiles.Find(git, root, ct));

    private static Result<ServiceCatalog> Read(IDirectory root, DeclarationFiles files)
    {
        var problems = new List<Error>(files.Problems);
        var nodes = new List<(DocumentSource Source, NodeDocument Document)>();
        var services = new List<(DocumentSource Source, ServiceDocument Document)>();
        var components = new List<(DocumentSource Source, ComponentDocument Document)>();
        foreach (var file in files.Files)
        {
            if (!YamlDocuments.Parse(file.Text).TryGetValue(out var yaml, out var unreadable))
            {
                problems.AddRange(unreadable.Select(error => CatalogError.In(new DocumentSource(file.Path), error)));
                continue;
            }

            for (var index = 0; index < yaml.Documents.Count; index++)
            {
                var document = yaml.Documents[index];
                var source = new DocumentSource(file.Path, index, yaml.Documents.Count);
                if (LabSchema.Validate(document) is { Count: > 0 } shape)
                {
                    problems.AddRange(shape.Select(problem => CatalogError.In(source, problem.Problem, problem.Line)));
                }
                else if (document.Root?["kind"]?.GetValue<string>() == "service")
                {
                    services.Add((source, Read<ServiceDocument>(document.Root)));
                }
                else if (document.Root?["kind"]?.GetValue<string>() == "node")
                {
                    nodes.Add((source, Read<NodeDocument>(document.Root)));
                }
                else
                {
                    components.Add((source, ComponentDocuments.Read(document.Root, Serializer)));
                }
            }
        }

        var catalog = new ServiceCatalog();
        foreach (var (source, document) in nodes)
        {
            AddNode(catalog, source, document, problems);
        }

        foreach (var (source, document) in InOrder(services, service => service.Document.Name, service => service.Document.DependsOn ?? []))
        {
            AddService(catalog, root, source, document, problems);
        }

        // A service's components among themselves: none refers outside its own service.
        foreach (var service in components.GroupBy(component => Owner(component.Source)))
        {
            foreach (var (source, document) in InOrder([.. service], component => component.Document.Name,
                         component => [.. component.Document.PartOf is { } whole ? [whole] : Array.Empty<string>(), .. component.Document.DependsOn ?? []]))
            {
                AddComponent(catalog, source, document, problems);
            }
        }

        return problems.Count == 0 ? catalog : problems;
    }

    /// <summary>
    /// <paramref name="declarations"/> so each comes after whatever it refers to, as far as that
    /// can be: one referring to something missing, or to something that refers back, comes last,
    /// where adding it is refused for it. Ties keep the order they were read in.
    /// </summary>
    private static IEnumerable<T> InOrder<T>(IReadOnlyList<T> declarations, Func<T, string> name, Func<T, IReadOnlyList<string>> refers)
    {
        var placed = new HashSet<string>(StringComparer.Ordinal);
        var waiting = declarations.ToList();
        // An index, not FirstOrDefault: a declaration may be a value, whose default is no "none".
        for (var at = waiting.FindIndex(Ready); at >= 0; at = waiting.FindIndex(Ready))
        {
            var next = waiting[at];
            waiting.RemoveAt(at);
            placed.Add(name(next));
            yield return next;
        }

        foreach (var stuck in waiting)
        {
            yield return stuck;
        }

        yield break;

        bool Ready(T declaration) => refers(declaration).All(placed.Contains);
    }

    // The service a component's file sits under, area/service: what groups it with its siblings.
    private static string Owner(DocumentSource source) => string.Join('/', source.Directories.Take(2));

    // The schema has judged the shape already, so this cannot fail on it.
    private static T Read<T>(JsonNode? node) =>
        node.Deserialize<T>(Serializer) ?? throw new InvalidOperationException($"A {typeof(T).Name} the schema passed did not deserialize.");

    private static void AddService(ServiceCatalog catalog, IDirectory root, DocumentSource source, ServiceDocument document, List<Error> problems)
    {
        var links = new List<ServiceLink>();
        foreach (var link in document.Links ?? [])
        {
            if (Link(link, root, source.File).TryGetValue(out var read, out var refused))
            {
                links.Add(read);
            }
            else
            {
                problems.AddRange(refused.Select(error => CatalogError.In(source, error)));
            }
        }

        var service = Service.Create(source, ServiceName.From(document.Name), [.. (document.DependsOn ?? []).Select(ServiceName.From)]);
        if (!service.TryGetValue(out var created, out var invalid))
        {
            problems.AddRange(invalid);
            return;
        }

        created.DisplayName = document.DisplayName;
        created.Description = document.Description;
        created.Lifecycle = document.Lifecycle ?? Lifecycle.Production;
        created.Links = links;
        problems.AddRange(catalog.Add(created).Errors ?? []);
    }

    private static void AddNode(ServiceCatalog catalog, DocumentSource source, NodeDocument document, List<Error> problems)
    {
        var drives = new List<HostPath>();
        foreach (var (drive, index) in (document.Drives ?? []).Select((drive, index) => (drive, index)))
        {
            var path = HostPath.TryFrom(drive);
            if (path.IsSuccess)
            {
                drives.Add(path.ValueObject);
            }
            else
            {
                problems.Add(CatalogError.In(source, new FieldError($"drives.{index}", DeclarationErrors.Schema(path.Error.ErrorMessage))));
            }
        }

        var address = HostName.TryFrom(document.Address);
        if (!address.IsSuccess)
        {
            problems.Add(CatalogError.In(source, new FieldError("address", DeclarationErrors.Schema(address.Error.ErrorMessage))));
        }

        var docker = document.Docker is { } written ? DockerHost.TryFrom(written) : null;
        if (docker is { IsSuccess: false } refused)
        {
            problems.Add(CatalogError.In(source, new FieldError("docker", DeclarationErrors.Schema(refused.Error.ErrorMessage))));
        }

        var root = AbsolutePath.TryFrom(document.Root);
        var data = AbsolutePath.TryFrom(document.Data);
        foreach (var (field, path) in new[] { ("root", root), ("data", data) }.Where(path => !path.Item2.IsSuccess))
        {
            problems.Add(CatalogError.In(source, new FieldError(field, DeclarationErrors.Schema(path.Error.ErrorMessage))));
        }

        if (!address.IsSuccess || docker is { IsSuccess: false } || !root.IsSuccess || !data.IsSuccess)
        {
            return;
        }

        var node = Node.Create(source, NodeName.From(document.Name), document.Role, document.Platform, address.ValueObject,
            new NodeDirectories(root.ValueObject, data.ValueObject));
        if (!node.TryGetValue(out var created, out var invalid))
        {
            problems.AddRange(invalid);
            return;
        }

        created.Docker = docker?.ValueObject;
        created.Drives = drives;
        problems.AddRange(catalog.Add(created).Errors ?? []);
    }

    private static void AddComponent(ServiceCatalog catalog, DocumentSource source, ComponentDocument document, List<Error> problems)
    {
        var name = ComponentName.From(document.Name);
        var partOf = document.PartOf is { } whole ? ComponentName.From(whole) : (ComponentName?)null;
        IReadOnlyList<ComponentName> dependsOn = [.. (document.DependsOn ?? []).Select(ComponentName.From)];
        if (document is AgentsDocument agents)
        {
            AgentPackage? package = null;
            if (agents.Package is { } declaredPackage)
            {
                if (!Package(declaredPackage).TryGetValue(out package, out var unpackaged))
                {
                    problems.AddRange(unpackaged.Select(error => CatalogError.In(source, error)));
                    return;
                }
            }

            var declared = new AgentProcess
            {
                Name = AgentName.From(agents.Agent),
                Package = package,
                Program = Template.From(agents.Program),
                Arguments = [.. (agents.Arguments ?? []).Select(Template.From)],
                Environment = (agents.Environment ?? []).ToDictionary(variable => variable.Key, variable => Template.From(variable.Value), StringComparer.Ordinal),
                Supersedes = agents.Supersedes ?? []
            };

            if (!Placed(agents.RunsOn).TryGetValue(out var runsOn, out var unplaced))
            {
                problems.AddRange(unplaced.Select(error => CatalogError.In(source, new FieldError("runsOn", error))));
                return;
            }

            AddTo(catalog, AgentComponent.Create(source, name, document.Kind, document.Workflow, partOf, dependsOn, runsOn, declared), document, problems);
            return;
        }

        if (document is not ComposeDocument compose)
        {
            AddTo(catalog, Component.Create(source, name, document.Kind, document.Workflow, partOf, dependsOn), document, problems);
            return;
        }

        var errors = new List<Error>();
        var service = ComposeServiceName.TryFrom(compose.Service);
        if (!service.IsSuccess)
        {
            errors.Add(new FieldError("service", ComponentErrors.NotAComposeService(compose.Service)));
        }

        var metrics = new List<MetricsEndpoint>();
        var endpoints = compose.Metrics?.Endpoints ?? [];
        foreach (var (written, index) in endpoints.Select((written, index) => (written, index)))
        {
            // As it is written: one endpoint is the facet itself, several each have an index.
            var at = endpoints.Count > 1 ? $"metrics.{index}" : "metrics";
            if (Endpoint(written).TryGetValue(out var endpoint, out var refused))
            {
                metrics.Add(endpoint);
            }
            else
            {
                errors.AddRange(refused.Select(error => new FieldError($"{at}.path", error)));
            }
        }

        if (errors.Count > 0)
        {
            problems.AddRange(errors.Select(error => CatalogError.In(source, error)));
            return;
        }

        var created = ComposeComponent.Create(source, name, document.Kind, document.Workflow, partOf, dependsOn, service.ValueObject);
        if (created.Value is { } component)
        {
            component.Logs = compose.Logs;
            component.Metrics = metrics;
        }

        AddTo(catalog, created, document, problems);
    }

    // A component made, described as its document says, and added to the service whose directory holds its file.
    private static void AddTo<T>(ServiceCatalog catalog, Result<T> created, ComponentDocument document, List<Error> problems) where T : Component
    {
        if (!created.TryGetValue(out var component, out var refused))
        {
            problems.AddRange(refused);
            return;
        }

        component.DisplayName = document.DisplayName;
        component.Description = document.Description;
        if (!catalog.ServiceDeclaring(component.Source).TryGetValue(out var owner, out var unowned))
        {
            problems.AddRange(unowned);
            return;
        }

        problems.AddRange(owner.Add(component).Errors ?? []);
    }

    // An agent's package: the schema holds the repository to owner/name already, and the domain
    // judges both it and the version.
    private static Result<AgentPackage> Package(AgentPackageDocument document)
    {
        var repository = GitHubRepository.TryFrom(document.Github);
        var version = PackageVersion.TryFrom(document.Version);
        var errors = new List<Error>();
        if (!repository.IsSuccess)
        {
            errors.Add(new FieldError("package.github", DeclarationErrors.Schema(repository.Error.ErrorMessage)));
        }

        if (!version.IsSuccess)
        {
            errors.Add(new FieldError("package.version", DeclarationErrors.Schema(version.Error.ErrorMessage)));
        }

        return errors.Count > 0
            ? errors
            : new AgentPackage(repository.ValueObject, version.ValueObject, Template.From(document.Asset),
                document.Checksums is { } checksums ? Template.From(checksums) : null);
    }

    // The schema holds a rule to "all" or "every <word>" and a list to strings; the domain judges each.
    private static Result<DeploymentTarget> Placed(DeploymentTargetDocument document)
    {
        if (document.Rule is { } rule)
        {
            return DeploymentTarget.Rule(rule);
        }

        var written = document.Nodes ?? [];
        if (written.FirstOrDefault(name => !NodeName.TryFrom(name).IsSuccess) is { } invalid)
        {
            return DeclarationErrors.NotAName(invalid);
        }

        return DeploymentTarget.On([.. written.Select(NodeName.From)]);
    }

    /// <summary>
    /// A link to an absolute URL, or to a path in the checkout that is there.
    /// </summary>
    private static Result<ServiceLink> Link(LinkDocument link, IDirectory root, RepositoryPath file)
    {
        switch (link)
        {
            case { Url: { } url, Path: null }:
                return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
                    ? new ServiceLink(link.Title, link.Type, uri)
                    : ServiceLinkErrors.NotAnHttpUrl(link.Title, url);
            case { Url: null, Path: { } path }:
                // Rooted, a path would be this machine's rather than the repository's, and
                // Path.Combine would quietly drop the checkout for it.
                if (Path.IsPathRooted(path) || !Uri.TryCreate(path, UriKind.Relative, out var relative))
                {
                    return ServiceLinkErrors.NotARelativePath(link.Title, path);
                }

                if (RepositoryPath.Resolve(file.Parent, path) is not { } resolved)
                {
                    return ServiceLinkErrors.OutOfTheCheckout(link.Title, path);
                }

                return resolved.FileIn(root).Exists || resolved.DirectoryIn(root).Exists
                    ? new ServiceLink(link.Title, link.Type, relative)
                    : ServiceLinkErrors.NotInTheCheckout(link.Title, path);
            default:
                return ServiceLinkErrors.UrlOrPath(link.Title);
        }
    }

    // The schema holds the port to a port already; the path is the domain's to judge.
    private static Result<MetricsEndpoint> Endpoint(MetricsDocument document)
    {
        var published = document.Published is { } port ? Port.From(port) : (Port?)null;
        if (document.Path is not { } written)
        {
            return new MetricsEndpoint(Port.From(document.Port), HttpPath.Metrics) { Published = published };
        }

        var path = HttpPath.TryFrom(written);
        return path.IsSuccess
            ? new MetricsEndpoint(Port.From(document.Port), path.ValueObject) { Published = published }
            : NetworkErrors.NotAPath(written);
    }
}
