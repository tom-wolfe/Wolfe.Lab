using Wolfe.Lab.Application.Workflows.ForgejoRunner.Jobs;

namespace Wolfe.Lab.Application.Workflows.ForgejoRunner;

/// <summary>
/// Forgejo's Actions runners: <c>"workflow": "forgejo-runner"</c>.
/// </summary>
public sealed class ForgejoRunnerWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "forgejo-runner";

    /// <inheritdoc />
    public override string Label => "forgejo runner";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new RegisterJob()];
}
