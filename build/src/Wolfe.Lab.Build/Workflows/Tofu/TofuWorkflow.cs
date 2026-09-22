using Wolfe.Lab.Build.Workflows.Tofu.Jobs;

namespace Wolfe.Lab.Build.Workflows.Tofu;

/// <summary>
/// A root module: planned on the pull request, applied on the merge.
/// </summary>
public sealed class TofuWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "tofu";

    /// <inheritdoc />
    public string Label => "tofu";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new CheckJob(), new DeployJob()];
}
