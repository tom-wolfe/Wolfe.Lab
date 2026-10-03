using System.Text.Json;
using System.Text.Json.Nodes;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// Reads a checkout's declarations into the <see cref="Catalog"/>, in four stages, each with
/// problems of its own: the YAML; its shape, against <see cref="LabSchema"/>; each value, as the
/// domain's types take it; and the documents together, as the catalog holds them.
/// </summary>
/// <remarks>
/// Every declaration is read, so a reference can be resolved whoever declares it; and each problem
/// keeps the document it is in, so a component's check can be held to its own.
/// </remarks>
public static class DeclarationReader
{
    // Each closed set reads itself from its value, through its own converter.
    private static readonly JsonSerializerOptions Serializer = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Everything the checkout at <paramref name="root"/> declares that holds, and every problem.
    /// </summary>
    public static async Task<CatalogResult> Read(ICommandRunner commands, string root, CancellationToken ct = default) =>
        Read(root, await DeclarationFiles.Find(commands, root, ct));

    /// <summary>
    /// Everything <paramref name="files"/> declare that holds, and every problem;
    /// <paramref name="root"/> is where a link's path is looked for.
    /// </summary>
    private static CatalogResult Read(string root, DeclarationFiles files)
    {
        var problems = new List<CatalogError>(files.Problems);
        var services = new List<Declared<Service>>();
        var components = new List<Declared<Component>>();
        foreach (var file in files.Files)
        {
            if (!YamlDocuments.Parse(file.Text).TryGetValue(out var yaml, out var unreadable))
            {
                problems.AddRange(unreadable.Select(error => new CatalogError(new DocumentSource(file.Path), error.Message)));
                continue;
            }

            for (var index = 0; index < yaml.Documents.Count; index++)
            {
                var document = yaml.Documents[index];
                var source = new DocumentSource(file.Path, index, yaml.Documents.Count);
                var kind = document.Root?["kind"] is JsonValue value && value.TryGetValue<string>(out var named) ? named : null;
                var subject = kind switch
                {
                    "service" => ErrorSubject.Service,
                    _ when ComponentKind.All.Any(known => known.Value == kind) => ErrorSubject.Component,
                    _ => ErrorSubject.Unknown
                };

                if (LabSchema.Judge(document) is { Count: > 0 } shape)
                {
                    problems.AddRange(shape.Select(problem => new CatalogError(source, problem.Problem, problem.Line, subject)));
                    continue;
                }

                if (subject == ErrorSubject.Service)
                {
                    Take(Service(Read<ServiceDocument>(document.Root), root, file.Path), source, subject, services, problems);
                }
                else
                {
                    Take(Component(Read<ComponentDocument>(document.Root)), source, subject, components, problems);
                }
            }
        }

        return Catalog.Read(services, components).With(problems);
    }

    private static void Take<T>(Result<T> declaration, DocumentSource source, ErrorSubject subject, List<Declared<T>> into, List<CatalogError> problems) where T : class
    {
        if (declaration.TryGetValue(out var value, out var refused))
        {
            into.Add(new Declared<T>(value, source));
        }
        else
        {
            problems.AddRange(refused.Select(error => new CatalogError(source, error.Message, Subject: subject)));
        }
    }

    // The schema has judged the shape already, so this cannot fail on it.
    private static T Read<T>(JsonNode? node) =>
        node.Deserialize<T>(Serializer) ?? throw new InvalidOperationException($"A {typeof(T).Name} the schema passed did not deserialize.");

    private static Result<Service> Service(ServiceDocument document, string root, RepositoryPath file)
    {
        var errors = new List<Error>();
        var links = new List<ServiceLink>();
        foreach (var link in document.Links ?? [])
        {
            if (Link(link, root, file).TryGetValue(out var read, out var refused))
            {
                links.Add(read);
            }
            else
            {
                errors.AddRange(refused);
            }
        }

        return errors.Count > 0
            ? errors
            : new Service(ServiceName.From(document.Name), document.DisplayName, document.Description, document.Lifecycle ?? Lifecycle.Production, links,
                [.. (document.DependsOn ?? []).Select(ServiceName.From)]);
    }

    /// <summary>
    /// A link to an absolute URL, or to a path in the checkout that is there.
    /// </summary>
    private static Result<ServiceLink> Link(LinkDocument link, string root, RepositoryPath file)
    {
        switch (link)
        {
            case { Url: { } url, Path: null }:
                return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
                    ? new ServiceLink(link.Title, link.Type, uri)
                    : new Error($"the link '{link.Title}' has the url '{url}', which is not an absolute http or https URL.");
            case { Url: null, Path: { } path }:
                // Rooted, a path would be this machine's rather than the repository's, and
                // Path.Combine would quietly drop the checkout for it.
                if (Path.IsPathRooted(path) || !Uri.TryCreate(path, UriKind.Relative, out var relative))
                {
                    return new Error($"the link '{link.Title}' has the path '{path}', which is not a relative path.");
                }

                if (RepositoryPath.Resolve(file.Parent, path) is not { } resolved)
                {
                    return new Error($"the link '{link.Title}' has the path '{path}', which leads out of the checkout.");
                }

                var target = Path.Combine(root, resolved.Value);

                return File.Exists(target) || Directory.Exists(target)
                    ? new ServiceLink(link.Title, link.Type, relative)
                    : new Error($"the link '{link.Title}' has the path '{path}', which is not in the checkout.");
            default:
                return new Error($"the link '{link.Title}' needs a url or a path, not both.");
        }
    }

    private static Result<Component> Component(ComponentDocument document) =>
        ComponentType.Of(document.Kind, document.Type).TryGetValue(out var type, out var unknown)
            ? new Component(document.Name is { } name ? ComponentName.From(name) : null, document.DisplayName, document.Description, type,
                [.. (document.DependsOn ?? []).Select(ComponentName.From)])
            : unknown;
}
