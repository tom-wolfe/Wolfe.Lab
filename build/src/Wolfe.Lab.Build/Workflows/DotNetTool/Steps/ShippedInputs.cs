namespace Wolfe.Lab.Build.Workflows.DotNetTool.Steps;

/// <summary>
/// What decides the package's contents, relative to the component: the project's directory, the
/// repository-wide build properties and package versions, and the SDK.
/// </summary>
internal sealed record ShippedInputs(IReadOnlyList<string> Paths)
{
    public static ShippedInputs For(string project) =>
        new([Path.GetDirectoryName(project) is { Length: > 0 } directory ? directory : ".", "Directory.Build.props", "Directory.Packages.props", "global.json"]);
}
