using Wolfe.Lab.Workflows.ForgejoRunners.Jobs;

namespace Wolfe.Lab.Workflows.ForgejoRunners;

/// <summary>
/// Forgejo's Actions runners: <c>"workflow": "forgejo-runners"</c>.
/// </summary>
public sealed class ForgejoRunnersWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "forgejo-runners";

    /// <inheritdoc />
    public string Label => "forgejo runners";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new RegisterJob()];
}
