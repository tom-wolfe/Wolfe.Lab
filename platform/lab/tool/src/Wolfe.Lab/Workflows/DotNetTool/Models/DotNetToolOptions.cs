namespace Wolfe.Lab.Workflows.DotNetTool.Models;

/// <summary>
/// The shape of a .NET tool component's <c>ritten.json</c>: <c>"workflow": "dotnet-tool"</c>.
/// </summary>
public sealed record DotNetToolOptions : WorkflowSettings
{
    /// <summary>
    /// The tool's project file, relative to the component. Its version is the release's.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    /// The configuration to build, test and pack in.
    /// </summary>
    public string Configuration { get; init; } = "Release";

    /// <summary>
    /// Where the package is published.
    /// </summary>
    public FeedOptions Feed { get; init; } = new();
}
