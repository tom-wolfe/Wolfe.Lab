using Wolfe.Lab.Build.Obsidian.Jobs;

namespace Wolfe.Lab.Build.Obsidian.Workflows;

/// <summary>
/// Mirrors the Obsidian vaults into git: <c>"workflow": "obsidian"</c>.
/// </summary>
public sealed class ObsidianWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "obsidian";

    /// <inheritdoc />
    public string Label => "obsidian";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new SyncJob()];


}
