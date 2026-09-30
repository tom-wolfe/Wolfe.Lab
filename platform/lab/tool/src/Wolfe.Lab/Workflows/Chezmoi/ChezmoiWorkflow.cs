using Wolfe.Lab.Workflows.Chezmoi.Jobs;

namespace Wolfe.Lab.Workflows.Chezmoi;

/// <summary>
/// The machine plane: every profile rendered on the pull request, every node updated on the merge.
/// </summary>
public sealed class ChezmoiWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "chezmoi";

    /// <inheritdoc />
    public string Label => "chezmoi";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new CheckJob(), new DeployJob()];
}
