using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Ritten.Git;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Backups;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Components.Backups;
using Wolfe.Lab.Domain.Catalog.Components.Caddy;
using Wolfe.Lab.Domain.Catalog.Components.Chezmoi;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Domain.Catalog.Components.Forgejo;
using Wolfe.Lab.Domain.Catalog.Components.Garage;
using Wolfe.Lab.Domain.Catalog.Components.Images;
using Wolfe.Lab.Domain.Catalog.Components.Models;
using Wolfe.Lab.Domain.Catalog.Components.Obsidian;
using Wolfe.Lab.Domain.Catalog.Components.Restic;
using Wolfe.Lab.Domain.Catalog.Facets.Heartbeats;
using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Git;
using Wolfe.Lab.Domain.Network;
using Wolfe.Lab.Domain.Packages;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Domain.Secrets;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// Reads the service catalog from a Git repository.
/// </summary>
public static partial class ServiceCatalogReader
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
                         component => [.. component.Document.PartOf is { } whole ? [whole] : Array.Empty<string>(), .. component.Document.DependsOn ?? [],
                             .. (component.Document as ModelDocument)?.ServedBy.Keys ?? Enumerable.Empty<string>()]))
            {
                AddComponent(catalog, source, document, problems);
            }
        }

        // Only once every component is in: the head is the one none of the others names, and a
        // model is served by every server any of its service's models names.
        if (problems.Count == 0)
        {
            problems.AddRange(catalog.DeploymentUnits.SelectMany(unit => unit.Errors ?? []));
            problems.AddRange(catalog.Services.SelectMany(ModelComponent.Unserved));
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
        var errors = new List<Error>();
        var name = ComponentName.From(document.Name);
        var created = document switch
        {
            ModelDocument model => Model(source, name, model, errors),
            BackupDocument backup => Backup(source, name, backup, errors),
            ResticDocument restic => Restic(source, name, restic, errors),
            AgentDocument agent => Agent(source, name, agent, errors),
            DockerDocument docker => Docker(source, name, docker, errors),
            ChezmoiDocument chezmoi => Chezmoi(source, name, chezmoi, errors),
            ImageDocument image => Image(source, name, image, errors),
            CaddyCertificatesDocument certificates => CaddyCertificates(source, name, certificates, errors),
            ObsidianDocument vault => Obsidian(source, name, vault, errors),
            ForgejoRunnerDocument runners => Widen(ForgejoRunnerComponent.Create(source, name, runners.Kind, runners.Vault, runners.Repository, runners.Image), _ => { }),
            _ => Component.Create(source, name, document.Kind, document.Workflow)
        };

        if (errors.Count > 0)
        {
            problems.AddRange(errors.Select(error => CatalogError.In(source, error)));
            return;
        }

        AddTo(catalog, created!, document, source, problems);
    }

    // A model: each server's overrides, and the defaults, read as the domain's values.
    private static Result<Component>? Model(DocumentSource source, ComponentName name, ModelDocument document, List<Error> errors)
    {
        var servedBy = new Dictionary<ComponentName, ModelServing>();
        foreach (var (server, serving) in document.ServedBy)
        {
            if (!ComponentName.TryFrom(server).IsSuccess)
            {
                errors.Add(new FieldError("servedBy", DeclarationErrors.NotAName(server)));
                continue;
            }

            servedBy[ComponentName.From(server)] = new ModelServing(Tag($"servedBy.{server}.model", serving?.Model, errors), Context($"servedBy.{server}.context", serving?.Context, errors));
        }

        var model = Tag("model", document.Model, errors);
        var context = Context("context", document.Context, errors);
        return errors.Count > 0 ? null : Widen(ModelComponent.Create(source, name, document.Kind, servedBy), created =>
        {
            created.Model = model;
            created.Context = context;
        });
    }

    // A backup: what it snapshots, and what it leaves out and verifies of that.
    private static Result<Component>? Backup(DocumentSource source, ComponentName name, BackupDocument document, List<Error> errors)
    {
        var paths = HostPaths("paths", document.Paths, errors);
        var excludes = HostPaths("excludes", document.Excludes, errors);
        var verify = HostPaths("verify", document.Verify, errors);
        return errors.Count > 0 ? null : Widen(BackupComponent.Create(source, name, document.Kind, paths), created =>
        {
            created.Excludes = excludes;
            created.Verify = verify;
            created.Warm = document.Warm ?? false;
        });
    }

    // The repositories: what a prune keeps of them, and how much of the offsite copy a check reads back.
    private static Result<Component>? Restic(DocumentSource source, ComponentName name, ResticDocument document, List<Error> errors)
    {
        var retention = Retention(document.Retention, errors);
        var sample = document.Verify is { } verify ? Sample(verify.ReadDataSubset, errors) : null;
        return errors.Count > 0 || retention is null ? null : Widen(ResticComponent.Create(source, name, document.Kind, retention), created =>
        {
            if (sample is { } read)
            {
                created.VerifySample = read;
            }
        });
    }

    // A retention policy: each count a count, and the policy one that keeps something.
    private static RetentionPolicy? Retention(RetentionDocument written, List<Error> errors)
    {
        var counts = new[] { ("daily", written.Daily), ("weekly", written.Weekly), ("monthly", written.Monthly) }
            .Select(count => (Field: count.Item1, Count: SnapshotCount.TryFrom(count.Item2)))
            .ToList();
        foreach (var (field, count) in counts.Where(count => !count.Count.IsSuccess))
        {
            errors.Add(new FieldError($"retention.{field}", DeclarationErrors.Schema(count.Error.ErrorMessage)));
        }

        if (errors.Count > 0)
        {
            return null;
        }

        if (!RetentionPolicy.Create(counts[0].Count.ValueObject, counts[1].Count.ValueObject, counts[2].Count.ValueObject).TryGetValue(out var policy, out var keepsNothing))
        {
            errors.AddRange(keepsNothing);
            return null;
        }

        return policy with { KeepTags = written.KeepTags ?? [] };
    }

    // A share of data read back, as restic spells it: 5%.
    private static Percentage? Sample(string written, List<Error> errors)
    {
        var share = int.TryParse(written.TrimEnd('%'), CultureInfo.InvariantCulture, out var percent) ? Percentage.TryFrom(percent) : null;
        if (share is { IsSuccess: true })
        {
            return share.ValueObject;
        }

        errors.Add(new FieldError("verify.readDataSubset", DeclarationErrors.Schema(share?.Error.ErrorMessage ?? $"'{written}' is no percentage.")));
        return null;
    }

    // An agent: the process it runs, and the nodes it runs on.
    private static Result<Component>? Agent(DocumentSource source, ComponentName name, AgentDocument document, List<Error> errors)
    {
        AgentPackage? package = null;
        if (document.Package is { } declaredPackage && !Package(declaredPackage).TryGetValue(out package, out var unpackaged))
        {
            errors.AddRange(unpackaged);
            return null;
        }

        if (!Placed(document.RunsOn).TryGetValue(out var runsOn, out var unplaced))
        {
            errors.AddRange(unplaced.Select(error => new FieldError("runsOn", error)));
            return null;
        }

        var process = new AgentProcess
        {
            Name = AgentName.From(document.Agent),
            Package = package,
            Program = Template.From(document.Program),
            Arguments = [.. (document.Arguments ?? []).Select(Template.From)],
            Environment = (document.Environment ?? []).ToDictionary(variable => variable.Key, variable => Template.From(variable.Value), StringComparer.Ordinal),
            Supersedes = document.Supersedes ?? []
        };
        return Widen(AgentComponent.Create(source, name, document.Kind, runsOn, process), _ => { });
    }

    // The machines' profiles, each a profile's name.
    private static Result<Component>? Chezmoi(DocumentSource source, ComponentName name, ChezmoiDocument document, List<Error> errors)
    {
        var profiles = new List<ChezmoiProfile>();
        foreach (var (written, index) in document.Profiles.Select((written, index) => (written, index)))
        {
            var profile = ChezmoiProfile.TryFrom(written);
            if (profile.IsSuccess)
            {
                profiles.Add(profile.ValueObject);
            }
            else
            {
                errors.Add(new FieldError($"profiles.{index}", DeclarationErrors.Schema(profile.Error.ErrorMessage)));
            }
        }

        return errors.Count > 0 ? null : Widen(ChezmoiComponent.Create(source, name, document.Kind, profiles), _ => { });
    }

    // An image: where it is pushed, and what it is built from.
    private static Result<Component>? Image(DocumentSource source, ComponentName name, ImageDocument document, List<Error> errors)
    {
        var tag = ImageTag.TryFrom(document.Tag);
        if (!tag.IsSuccess)
        {
            errors.Add(new FieldError("tag", DeclarationErrors.Schema(tag.Error.ErrorMessage)));
            return null;
        }

        return Widen(ImageComponent.Create(source, name, document.Kind, tag.ValueObject), created =>
        {
            created.Context = document.Context ?? created.Context;
            created.Dockerfile = document.Dockerfile ?? created.Dockerfile;
        });
    }

    // A certificate: what it covers, and what issues it.
    private static Result<Component>? CaddyCertificates(DocumentSource source, ComponentName name, CaddyCertificatesDocument document, List<Error> errors)
    {
        var written = document.Issuer;
        var store = Value("issuer.store", HostPath.TryFrom(written.Store), errors);
        var environment = new Dictionary<string, SecretReference>(StringComparer.Ordinal);
        foreach (var (variable, reference) in written.Environment ?? [])
        {
            if (Value($"issuer.environment.{variable}", SecretReference.TryFrom(reference), errors) is { } secret)
            {
                environment[variable] = secret;
            }
        }

        if (errors.Count > 0 || store is null)
        {
            return null;
        }

        var issuer = new CertificateIssuer(written.Image, written.Email, written.Dns, store.Value) { Environment = environment, PropagationWait = written.PropagationWait };
        return Widen(CaddyCertificatesComponent.Create(source, name, document.Kind, document.Domains, issuer), _ => { });
    }

    // A vault: where it is checked out, where it is pushed, and as whom.
    private static Result<Component>? Obsidian(DocumentSource source, ComponentName name, ObsidianDocument document, List<Error> errors)
    {
        var path = Value("path", HostPath.TryFrom(document.Path), errors);
        var repository = Value("repository", RepositoryUrl.TryFrom(document.Repository), errors);
        var username = Value("push.username", GitUsername.TryFrom(document.Push.Username), errors);
        var token = Value("push.token", SecretReference.TryFrom(document.Push.Token), errors);
        if (path is not { } at || repository is not { } url || username is not { } user || token is not { } secret)
        {
            return null;
        }

        return Widen(ObsidianComponent.Create(source, name, document.Kind, at, url, new PushCredential(user, secret)), created => created.Exclude = document.Exclude ?? []);
    }

    // A value as the domain reads it, or null and the problem at its field.
    private static T? Value<T>(string field, Vogen.ValueObjectOrError<T> read, List<Error> errors) where T : struct
    {
        if (read.IsSuccess)
        {
            return read.ValueObject;
        }

        errors.Add(new FieldError(field, DeclarationErrors.Schema(read.Error.ErrorMessage)));
        return null;
    }

    // A compose service: the service it runs as, and how it reports.
    private static Result<Component>? Docker(DocumentSource source, ComponentName name, DockerDocument document, List<Error> errors)
    {
        var service = ComposeServiceName.TryFrom(document.Service);
        if (!service.IsSuccess)
        {
            errors.Add(new FieldError("service", ComponentErrors.NotAComposeService(document.Service)));
        }

        var metrics = new List<MetricsEndpoint>();
        var endpoints = document.Metrics?.Endpoints ?? [];
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

        var layout = document is GarageDocument garage ? Layout(garage.Layout, errors) : null;
        if (errors.Count > 0)
        {
            return null;
        }

        void Reports(DockerComponent created)
        {
            created.Logs = document.Logs;
            created.Metrics = metrics;
        }

        return layout is not null
            ? Widen(GarageComponent.Create(source, name, document.Kind, service.ValueObject, layout), Reports)
            : document.Workflow == WorkflowName.DotNetService
                ? Widen(DotNetServiceComponent.Create(source, name, document.Kind, service.ValueObject), Reports)
                : Widen(DockerComponent.Create(source, name, document.Kind, service.ValueObject), Reports);
    }

    // A Garage node's role: the zone it stands in, and what it stores in bytes.
    private static GarageLayout? Layout(LayoutDocument written, List<Error> errors)
    {
        var zone = GarageZone.TryFrom(written.Zone);
        if (!zone.IsSuccess)
        {
            errors.Add(new FieldError("layout.zone", DeclarationErrors.Schema(zone.Error.ErrorMessage)));
        }

        var capacity = Capacity(written.Capacity, errors);
        return zone.IsSuccess && capacity is { } bytes ? new GarageLayout(zone.ValueObject, bytes) : null;
    }

    // A size as Garage reads one: 500G is decimal, 500GiB binary.
    private static StorageCapacity? Capacity(string written, List<Error> errors)
    {
        var problem = $"'{written}' is no capacity: a number of K, M, G, T or P (decimal) or Ki … Pi (binary), a B optional.";
        var match = CapacitySpelling().Match(written);
        if (!match.Success)
        {
            errors.Add(new FieldError("layout.capacity", DeclarationErrors.Schema(problem)));
            return null;
        }

        var power = "KMGTP".IndexOf(char.ToUpperInvariant(match.Groups["unit"].Value is { Length: > 0 } unit ? unit[0] : ' '), StringComparison.Ordinal) + 1;
        var size = match.Groups["binary"].Success ? 1024m : 1000m;
        var bytes = decimal.Parse(match.Groups["count"].Value, CultureInfo.InvariantCulture);
        for (var at = 0; at < power; at++)
        {
            bytes *= size;
        }

        var capacity = bytes <= long.MaxValue ? StorageCapacity.TryFrom((long)bytes) : null;
        if (capacity is not { IsSuccess: true })
        {
            errors.Add(new FieldError("layout.capacity", DeclarationErrors.Schema(capacity?.Error.ErrorMessage ?? problem)));
            return null;
        }

        return capacity.ValueObject;
    }

    [GeneratedRegex("^(?<count>[0-9]{1,15}) ?((?<unit>[KkMmGgTtPp])(?<binary>[Ii])?)?[Bb]?$")]
    private static partial Regex CapacitySpelling();

    // A component made and given what its document declares beyond what it is made with, as the
    // component it also is: a Result of the derived type is not one of its base.
    private static Result<Component> Widen<T>(Result<T> created, Action<T> declares) where T : Component
    {
        if (created.Value is not { } component)
        {
            return new Result<Component>(created.Errors ?? []);
        }

        declares(component);
        return component;
    }

    // A model's tag, or null when none is written; one that is no tag is a problem at its field.
    private static ModelTag? Tag(string field, string? written, List<Error> errors)
    {
        if (written is null)
        {
            return null;
        }

        var tag = ModelTag.TryFrom(written);
        if (!tag.IsSuccess)
        {
            errors.Add(new FieldError(field, DeclarationErrors.Schema(tag.Error.ErrorMessage)));
            return null;
        }

        return tag.ValueObject;
    }

    // A heartbeat, or null when none is written; a slug or a key that is none is a problem at its field.
    private static HeartbeatCheck? Heartbeat(HeartbeatDocument? written, List<Error> errors)
    {
        if (written is null)
        {
            return null;
        }

        var slug = HeartbeatSlug.TryFrom(written.Check);
        var key = SecretReference.TryFrom(written.Key);
        if (!slug.IsSuccess)
        {
            errors.Add(new FieldError("heartbeat.check", DeclarationErrors.Schema(slug.Error.ErrorMessage)));
        }

        if (!key.IsSuccess)
        {
            errors.Add(new FieldError("heartbeat.key", DeclarationErrors.Schema(key.Error.ErrorMessage)));
        }

        return slug.IsSuccess && key.IsSuccess ? new HeartbeatCheck(slug.ValueObject, key.ValueObject) : null;
    }

    // A context length, or null when none is written; one that is no length is a problem at its field.
    private static ContextLength? Context(string field, int? written, List<Error> errors)
    {
        if (written is not { } tokens)
        {
            return null;
        }

        var context = ContextLength.TryFrom(tokens);
        if (!context.IsSuccess)
        {
            errors.Add(new FieldError(field, DeclarationErrors.Schema(context.Error.ErrorMessage)));
            return null;
        }

        return context.ValueObject;
    }

    // A component made, described as its document says, and added to the service whose directory holds its file.
    private static void AddTo(ServiceCatalog catalog, Result<Component> created, ComponentDocument document, DocumentSource source, List<Error> problems)
    {
        var unmounted = new List<Error>();
        var volumes = HostPaths("requiresVolumes", document.RequiresVolumes, unmounted);
        var heartbeat = Heartbeat(document.Heartbeat, unmounted);
        problems.AddRange(unmounted.Select(error => CatalogError.In(source, error)));
        if (!created.TryGetValue(out var component, out var refused))
        {
            problems.AddRange(refused);
            return;
        }

        if (unmounted.Count > 0)
        {
            return;
        }

        component.DisplayName = document.DisplayName;
        component.Description = document.Description;
        component.PartOf = document.PartOf is { } whole ? ComponentName.From(whole) : null;
        component.DependsOn = [.. (document.DependsOn ?? []).Select(ComponentName.From)];
        component.RequiresVolumes = volumes;
        component.Heartbeat = heartbeat;
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

    // Paths on the node, each judged by the domain: a field's problems are added to errors, by index.
    private static IReadOnlyList<HostPath> HostPaths(string field, IReadOnlyList<string>? written, List<Error> errors)
    {
        var paths = new List<HostPath>();
        foreach (var (path, index) in (written ?? []).Select((path, index) => (path, index)))
        {
            var read = HostPath.TryFrom(path);
            if (read.IsSuccess)
            {
                paths.Add(read.ValueObject);
            }
            else
            {
                errors.Add(new FieldError($"{field}.{index}", DeclarationErrors.Schema(read.Error.ErrorMessage)));
            }
        }

        return paths;
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
