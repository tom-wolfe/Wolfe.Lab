using Wolfe.Lab.Build.Immich.Jobs;

namespace Wolfe.Lab.Build.Immich.Workflows;

/// <summary>
/// The photo library: <c>"workflow": "immich"</c>.
/// </summary>
public sealed class ImmichWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "immich";

    /// <inheritdoc />
    public string Label => "immich";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new DeployJob(), new ImportJob()];
}
