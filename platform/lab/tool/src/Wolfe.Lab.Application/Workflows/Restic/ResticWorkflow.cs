
using Wolfe.Lab.Application.Workflows.Restic.Jobs;

namespace Wolfe.Lab.Application.Workflows.Restic;

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
