using Wolfe.Lab.Application.Workflows.Tofu.Jobs;

namespace Wolfe.Lab.Application.Workflows.Tofu;

/// <summary>
/// A root module: planned on the pull request, applied on the merge.
/// </summary>
public sealed class TofuWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "tofu";

    /// <inheritdoc />
    public override string Label => "tofu";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new CheckJob(), new DeployJob()];
}
