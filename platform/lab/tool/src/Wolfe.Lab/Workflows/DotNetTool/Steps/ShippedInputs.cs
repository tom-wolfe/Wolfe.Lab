namespace Wolfe.Lab.Workflows.DotNetTool.Steps;

/// <summary>
/// What decides the package's contents, as git pathspecs from the component: the project's
/// directory, the repository-wide build properties and package versions, and the SDK — and
/// wherever those used to live, from the checkout's root.
/// </summary>
internal sealed record ShippedInputs(IReadOnlyList<string> Paths)
{
    public static ShippedInputs For(string project, IReadOnlyList<string> formerly) =>
        new([
            Path.GetDirectoryName(project) is { Length: > 0 } directory ? directory : ".",
            "Directory.Build.props",
            "Directory.Packages.props",
            "global.json",
            .. formerly.Select(path => $":(top){path}")
        ]);
}
