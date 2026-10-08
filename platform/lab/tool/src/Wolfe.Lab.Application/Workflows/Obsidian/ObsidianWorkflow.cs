
using Wolfe.Lab.Application.Workflows.Obsidian.Jobs;

namespace Wolfe.Lab.Application.Workflows.Obsidian;

/// <summary>
/// Mirrors the Obsidian vaults into git: <c>"workflow": "obsidian"</c>.
/// </summary>
public sealed class ObsidianWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "obsidian";

    /// <inheritdoc />
    public override string Label => "obsidian";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new SyncJob()];
}
