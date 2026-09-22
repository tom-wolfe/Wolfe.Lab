
using Wolfe.Lab.Build.Workflows.Restic.Jobs;

namespace Wolfe.Lab.Build.Workflows.Restic;

/// <summary>
/// The backup mechanism's own jobs.
/// </summary>
public sealed class ResticWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "restic";

    /// <inheritdoc />
    public string Label => "restic";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new OffsiteJob(), new Jobs.VerifyJob()];
}
