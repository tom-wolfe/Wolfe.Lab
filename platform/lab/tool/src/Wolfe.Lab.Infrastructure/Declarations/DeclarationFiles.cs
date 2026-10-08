using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Ritten.Git;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// The lab's declaration files in a checkout: every YAML file git tracks, three directories deep
/// at most, that names the lab's schema in its first line — whatever the file is called.
/// </summary>
/// <remarks>
/// Only what git tracks, because the repository is described at a commit, never by what lies in a
/// working directory; and only what names the schema, because every other tool's YAML —
/// <c>compose.yaml</c>, Loki's, Gatus's — is that tool's to read. The schema line is what gives an
/// editor the lab's rules, so one that names it by a path that does not lead to it is refused:
/// the file would be checked here and nowhere else.
/// </remarks>
/// <param name="Files">Each file and its contents, by its path from the root.</param>
/// <param name="Problems">Each file that names the schema but cannot be one of the lab's, and why.</param>
public sealed partial record DeclarationFiles(IReadOnlyList<DeclarationFile> Files, IReadOnlyList<CatalogError> Problems)
{
    /// <summary>
    /// How deep a declaration may be: <c>area/service/component/</c>.
    /// </summary>
    private const int Depth = 3;

    /// <summary>
    /// The declaration files in the checkout at <paramref name="root"/>, and every one that names
    /// the schema wrongly.
    /// </summary>
    public static async Task<DeclarationFiles> Find(IGit git, IDirectory root, CancellationToken ct = default)
    {
        var listed = await git.InRepository(root).TrackedFiles(["*.yaml", "*.yml"], ct);

        var files = new List<DeclarationFile>();
        var problems = new List<CatalogError>();
        foreach (var listing in listed.Order(StringComparer.Ordinal))
        {
            var path = RepositoryPath.From(listing);
            // Tracked, but deleted in the working directory: nothing to read, and nothing declared.
            if (await path.FileIn(root).ReadAllTextIfExists(ct) is not { } text)
            {
                continue;
            }

            if (SchemaLine().Match(text) is not { Success: true } line || !line.Groups["schema"].Value.EndsWith(LabSchema.Location.Segments[^1], StringComparison.Ordinal))
            {
                continue;
            }

            var named = RepositoryPath.Resolve(path.Parent, line.Groups["schema"].Value);
            if (path.Directories.Count > Depth)
            {
                problems.Add(new CatalogError(new DocumentSource(path), DeclarationErrors.TooDeep(Depth), 1));
            }
            else if (named != LabSchema.Location)
            {
                problems.Add(new CatalogError(new DocumentSource(path), DeclarationErrors.SchemaElsewhere(named, LabSchema.Location, LabSchema.Location.RelativeFrom(path.Parent)), 1));
            }
            else
            {
                files.Add(new DeclarationFile(path, text));
            }
        }

        return new DeclarationFiles(files, problems);
    }

    /// <summary>
    /// The workflow the components declared in <paramref name="directory"/> run — each that is not
    /// part of another of them names the same one — or null when they name none, or several.
    /// </summary>
    public static async Task<string?> WorkflowOf(IDirectory directory, CancellationToken ct = default)
    {
        var components = new List<JsonNode>();
        foreach (var file in directory.GetFiles("*.yaml").Concat(directory.GetFiles("*.yml")))
        {
            if (await file.ReadAllTextIfExists(ct) is { } text && SchemaLine().IsMatch(text) && YamlDocuments.Parse(text).TryGetValue(out var yaml, out _))
            {
                components.AddRange(yaml.Documents.Select(document => document.Root).OfType<JsonNode>().Where(root => root["workflow"] is not null));
            }
        }

        var names = components.Select(component => Text(component["name"])).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var workflows = components
            .Where(component => Text(component["partOf"]) is not { } whole || !names.Contains(whole))
            .Select(component => Text(component["workflow"]))
            .Distinct()
            .ToList();
        return workflows is [{ } only] ? only : null;
    }

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>
    /// The line a declaration starts with, for a file at <paramref name="path"/>.
    /// </summary>
    public static string SchemaLineFor(RepositoryPath path) => $"# yaml-language-server: $schema={LabSchema.Location.RelativeFrom(path.Parent)}";

    [GeneratedRegex(@"\A#\s*yaml-language-server:\s*\$schema=(?<schema>\S+)")]
    private static partial Regex SchemaLine();
}
