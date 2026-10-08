
using Wolfe.Lab.Application.Workflows.Restic.Jobs;

namespace Wolfe.Lab.Application.Workflows.Restic;

/// <summary>
/// The backup mechanism's own jobs.
/// </summary>
public sealed class ResticWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "restic";

    /// <inheritdoc />
    public override string Label => "restic";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new OffsiteJob(), new VerifyJob()];
}
