using Wolfe.Lab.Application.Workflows.ForgejoRunners.Jobs;

namespace Wolfe.Lab.Application.Workflows.ForgejoRunners;

/// <summary>
/// Forgejo's Actions runners: <c>"workflow": "forgejo-runners"</c>.
/// </summary>
public sealed class ForgejoRunnersWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "forgejo-runners";

    /// <inheritdoc />
    public override string Label => "forgejo runners";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new RegisterJob()];
}
