namespace Wolfe.Lab.Workflows.DotNetTool.Steps;

/// <summary>
/// What decides the package's contents: the project's directory and the build properties and
/// package versions beside it, relative to the component, and the SDK the repository pins at its
/// root, relative to that.
/// </summary>
internal sealed record ShippedInputs(IReadOnlyList<string> Component, IReadOnlyList<string> Repository)
{
    public static ShippedInputs For(string project) => new(
        [Path.GetDirectoryName(project) is { Length: > 0 } directory ? directory : ".", "Directory.Build.props", "Directory.Packages.props"],
        ["global.json"]);

    /// <summary>
    /// Every input as a path from the repository's root, for a component at <paramref name="component"/>
    /// within it — the one form git reads the same wherever it runs.
    /// </summary>
    public IReadOnlyList<string> FromRoot(string root, string component)
    {
        var at = Path.GetRelativePath(root, component);
        return [.. Component.Select(path => Normalise(Path.Combine(at, path))), .. Repository];
    }

    private static string Normalise(string path) => Path.GetRelativePath(".", path).Replace(Path.DirectorySeparatorChar, '/');
}
