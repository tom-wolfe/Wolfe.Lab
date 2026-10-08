using Wolfe.Lab.Application.Workflows.DotNetTool.Jobs;

namespace Wolfe.Lab.Application.Workflows.DotNetTool;

/// <summary>
/// A .NET tool the lab ships to its own feed: <c>"workflow": "dotnet-tool"</c>.
/// </summary>
public sealed class DotNetToolWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "dotnet-tool";

    /// <inheritdoc />
    public override string Label => "dotnet-tool";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new CheckJob(), new DeployJob()];
}
