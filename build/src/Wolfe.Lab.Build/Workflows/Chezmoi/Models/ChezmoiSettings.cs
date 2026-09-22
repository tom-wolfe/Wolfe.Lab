namespace Wolfe.Lab.Build.Workflows.Chezmoi.Models;

/// <summary>
/// What <c>chezmoi/ritten.json</c> declares: the profiles a machine can be.
/// </summary>
public sealed record ChezmoiSettings : WorkflowSettings
{
    /// <summary>
    /// Every profile the source is rendered for on a check — the prompt's list, so a template
    /// that only breaks on one machine fails before the merge.
    /// </summary>
    public IReadOnlyList<string> Profiles { get; init; } = [];
}
