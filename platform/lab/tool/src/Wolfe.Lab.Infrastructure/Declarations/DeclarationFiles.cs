using System.Text.RegularExpressions;
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
    public static async Task<DeclarationFiles> Find(ICommandRunner commands, IDirectory root, CancellationToken ct = default)
    {
        var listed = await commands.Run(Command.Create("git").WithArguments("ls-files", "-z", "--", "*.yaml", "*.yml")
            .InDirectory(root.AbsolutePath).QuietOutput().ThrowOnError(), ct);

        var files = new List<DeclarationFile>();
        var problems = new List<CatalogError>();
        // Ritten's runner ends what it captures with a newline, which is no file's name.
        foreach (var listing in listed.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Order(StringComparer.Ordinal))
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
    /// The line a declaration starts with, for a file at <paramref name="path"/>.
    /// </summary>
    public static string SchemaLineFor(RepositoryPath path) => $"# yaml-language-server: $schema={LabSchema.Location.RelativeFrom(path.Parent)}";

    [GeneratedRegex(@"\A#\s*yaml-language-server:\s*\$schema=(?<schema>\S+)")]
    private static partial Regex SchemaLine();
}
