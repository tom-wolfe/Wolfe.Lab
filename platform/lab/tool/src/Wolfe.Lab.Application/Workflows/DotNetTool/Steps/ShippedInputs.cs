using System.Xml.Linq;

namespace Wolfe.Lab.Application.Workflows.DotNetTool.Steps;

/// <summary>
/// What decides the package's contents: every project it is built from — the tool's own and each
/// project it references, followed through theirs — the build properties and package versions
/// beside the component, and the SDK the repository pins at its root.
/// </summary>
/// <param name="Project">The tool's project file, relative to the component.</param>
internal sealed record ShippedInputs(string Project)
{
    private static readonly string[] BesideTheComponent = ["Directory.Build.props", "Directory.Packages.props"];
    private static readonly string[] AtTheRoot = ["global.json"];

    public static ShippedInputs For(string project) => new(project);

    /// <summary>
    /// Every input as a path from the repository's root, for a component at <paramref name="component"/>
    /// within it — the one form git reads the same wherever it runs.
    /// </summary>
    /// <remarks>
    /// The projects are read from the component as it is now: a project the tool has stopped
    /// referencing no longer ships, and one it has started to is counted from its first release.
    /// </remarks>
    public IReadOnlyList<string> FromRoot(string root, string component)
    {
        var at = Path.GetRelativePath(root, component);
        var projects = Projects(Path.GetFullPath(Path.Combine(component, Project)))
            .Select(project => Path.GetRelativePath(component, Path.GetDirectoryName(project) ?? component));
        return [.. projects.Concat(BesideTheComponent).Select(path => Normalise(Path.Combine(at, path))).Distinct(), .. AtTheRoot];
    }

    /// <summary>
    /// The project and every project it references, each once, as absolute paths. A project file
    /// that is not there references nothing: its directory still ships, as it would once written.
    /// </summary>
    private static IEnumerable<string> Projects(string project)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>([project]);
        while (pending.TryDequeue(out var next))
        {
            if (!seen.Add(next))
            {
                continue;
            }

            yield return next;
            if (!File.Exists(next))
            {
                continue;
            }

            var directory = Path.GetDirectoryName(next) ?? ".";
            foreach (var reference in XDocument.Load(next).Descendants("ProjectReference"))
            {
                // MSBuild writes them with backslashes whatever the platform.
                if (reference.Attribute("Include")?.Value is { Length: > 0 } include)
                {
                    pending.Enqueue(Path.GetFullPath(Path.Combine(directory, include.Replace('\\', Path.DirectorySeparatorChar))));
                }
            }
        }
    }

    private static string Normalise(string path) => Path.GetRelativePath(".", path).Replace(Path.DirectorySeparatorChar, '/');
}
