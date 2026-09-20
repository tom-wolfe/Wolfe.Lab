using Wolfe.Lab.Build.Agents.Jobs;

namespace Wolfe.Lab.Build.Agents.Workflows;

/// <summary>
/// A slice whose stack is a supervised host process.
/// </summary>
public sealed class AgentsWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "agents";

    /// <inheritdoc />
    public string Label => "agents";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new ConvergeJob()];
}
