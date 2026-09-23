using Wolfe.Lab.Build.Workflows.DotNetTool.Jobs;

namespace Wolfe.Lab.Build.Workflows.DotNetTool;

/// <summary>
/// A .NET tool the lab ships to its own feed: <c>"workflow": "dotnet-tool"</c>.
/// </summary>
public sealed class DotNetToolWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "dotnet-tool";

    /// <inheritdoc />
    public string Label => "dotnet-tool";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new CheckJob(), new DeployJob()];
}
