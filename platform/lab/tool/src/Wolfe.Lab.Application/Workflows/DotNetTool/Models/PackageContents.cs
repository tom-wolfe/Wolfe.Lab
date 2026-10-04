using System.Xml.Linq;

namespace Wolfe.Lab.Application.Workflows.DotNetTool.Models;

/// <summary>
/// What decides the package's contents: every project it is built from — the tool's own and each
/// project it references, followed through theirs — the build properties and package versions
/// beside the component, and the SDK the repository pins at its root.
/// </summary>
/// <param name="Project">The tool's project file, relative to the component.</param>
internal sealed record PackageContents(string Project)
{
    private static readonly string[] BesideTheComponent = ["Directory.Build.props", "Directory.Packages.props"];
    private static readonly string[] AtTheRoot = ["global.json"];

    public static PackageContents For(string project) => new(project);

    /// <summary>
    /// Every input as a path from the repository's root, for a component at <paramref name="component"/>
    /// within it — the one form git reads the same wherever it runs.
    /// </summary>
    /// <remarks>
    /// The projects are read from the component as it is now: a project the tool has stopped
    /// referencing no longer ships, and one it has started to is counted from its first release.
    /// </remarks>
    public IReadOnlyList<string> FromRoot(IDirectory root, IDirectory component)
    {
        var at = root.RelativePath(component);
        var projects = Projects(component.GetFile(Project)).Select(project => component.RelativePath(project.Directory));
        return [.. projects.Concat(BesideTheComponent).Select(path => Normalise(Path.Combine(at, path))).Distinct(), .. AtTheRoot];
    }

    /// <summary>
    /// The project and every project it references, each once. A project file that is not there
    /// references nothing: its directory still ships, as it would once written.
    /// </summary>
    private static IEnumerable<IFile> Projects(IFile project)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<IFile>([project]);
        while (pending.TryDequeue(out var next))
        {
            if (!seen.Add(next.AbsolutePath))
            {
                continue;
            }

            yield return next;
            if (!next.Exists)
            {
                continue;
            }

            XDocument document;
            using (var stream = next.OpenRead())
            {
                document = XDocument.Load(stream);
            }

            foreach (var reference in document.Descendants("ProjectReference"))
            {
                // MSBuild writes them with backslashes whatever the platform.
                if (reference.Attribute("Include")?.Value is { Length: > 0 } include)
                {
                    pending.Enqueue(next.Directory.GetFile(include.Replace('\\', '/')));
                }
            }
        }
    }

    private static string Normalise(string path) => Path.GetRelativePath(".", path).Replace(Path.DirectorySeparatorChar, '/');
}
