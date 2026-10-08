using Wolfe.Lab.Application.Workflows.Chezmoi.Jobs;

namespace Wolfe.Lab.Application.Workflows.Chezmoi;

/// <summary>
/// The machine plane: every profile rendered on the pull request, every node updated on the merge.
/// </summary>
public sealed class ChezmoiWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "chezmoi";

    /// <inheritdoc />
    public override string Label => "chezmoi";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new CheckJob(), new DeployJob()];
}
