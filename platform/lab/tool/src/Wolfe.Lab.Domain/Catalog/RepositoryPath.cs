using Vogen;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// A path in the repository, relative to the repo root.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct RepositoryPath
{
    /// <summary>
    /// Its segments, the last being its file or directory's own name.
    /// </summary>
    public IReadOnlyList<string> Segments => Value.Split('/');

    /// <summary>
    /// The directories it sits in, from the root: <c>personal</c>, <c>immich</c>, <c>compose</c>.
    /// </summary>
    public IReadOnlyList<string> Directories => Segments.Count > 1 ? [.. Segments.Take(Segments.Count - 1)] : [];

    /// <summary>
    /// The directory it sits in, or null for one at the root.
    /// </summary>
    public RepositoryPath? Parent => Directories.Count > 0 ? From(string.Join('/', Directories)) : null;

    /// <summary>
    /// The path of <paramref name="name"/> within this directory.
    /// </summary>
    public RepositoryPath Combine(string name) => From($"{Value}/{name}");

    /// <summary>
    /// Whether <paramref name="path"/> is within this directory, at any depth — not merely
    /// starts with its name, as <c>personal/immich/comp</c> does <c>…/compose</c>.
    /// </summary>
    public bool Contains(RepositoryPath path) => path.Value.StartsWith(Value + "/", StringComparison.Ordinal);

    /// <summary>
    /// Where <paramref name="relative"/> leads from the directory <paramref name="from"/> — the
    /// root when null — with <c>..</c> and <c>.</c> taken as they are meant; or null when it is
    /// rooted, or leads above the root and so out of the repository.
    /// </summary>
    public static RepositoryPath? Resolve(RepositoryPath? from, string relative)
    {
        var path = relative.Replace('\\', '/');
        if (path.StartsWith('/') || (path.Length > 1 && path[1] == ':'))
        {
            return null;
        }

        var segments = new List<string>(from?.Segments ?? []);
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            switch (segment)
            {
                case ".":
                    break;
                case "..":
                    if (segments.Count == 0)
                    {
                        return null;
                    }

                    segments.RemoveAt(segments.Count - 1);
                    break;
                default:
                    segments.Add(segment);
                    break;
            }
        }

        return segments.Count > 0 ? From(string.Join('/', segments)) : null;
    }

    /// <summary>
    /// The relative path that leads here from the directory <paramref name="from"/> — the root
    /// when null — as a file in that directory would write it: <c>../../platform/lab/…</c>.
    /// </summary>
    public string RelativeFrom(RepositoryPath? from)
    {
        var start = from?.Segments ?? [];
        var shared = start.Zip(Segments).TakeWhile(pair => pair.First == pair.Second).Count();
        return string.Join('/', Enumerable.Repeat("..", start.Count - shared).Concat(Segments.Skip(shared)));
    }

    private static string NormalizeInput(string input) => input.Replace('\\', '/').TrimEnd('/');

    private static Validation Validate(string input) =>
        input.Length == 0 || input.StartsWith('/') || (input.Length > 1 && input[1] == ':')
            ? Validation.Invalid($"'{input}' is not a path in the repository: it is relative to the repository's root.")
            : input.Split('/').Any(segment => segment is "" or "." or "..")
                ? Validation.Invalid($"'{input}' is not a path in the repository: one names each directory on the way down, with no '.' or '..'.")
                : Validation.Ok;
}
