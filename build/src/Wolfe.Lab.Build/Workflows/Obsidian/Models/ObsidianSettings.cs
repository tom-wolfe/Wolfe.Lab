namespace Wolfe.Lab.Build.Workflows.Obsidian.Models;

/// <summary>
/// The shape of <c>obsidian/ritten.json</c>.
/// </summary>
public sealed record ObsidianSettings : WorkflowSettings
{
    /// <summary>
    /// How commits reach Forgejo.
    /// </summary>
    public PushSettings Push { get; init; } = new();

    /// <summary>
    /// The vaults the mini mirrors, by the name the <c>--vault</c> argument uses.
    /// </summary>
    public Dictionary<string, VaultSettings> Vaults { get; init; } = [];

    /// <summary>
    /// Paths git leaves out of every vault, written to each checkout's <c>.git/info/exclude</c>.
    /// </summary>
    public IReadOnlyList<string> Exclude { get; init; } = [];
}
