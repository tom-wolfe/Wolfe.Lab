namespace Wolfe.Lab.Build.Workflows.Forgejo.Models;

/// <summary>
/// The <c>runners</c> section: what every node's registration shares.
/// </summary>
/// <remarks>
/// A node's secret is the vault item <c>forgejo-runner-&lt;name&gt;</c> in this vault, minted by
/// Forgejo and kept in the vault as the origin; the node reads it once through a <c>create_</c>
/// template, and this job reads it to tell the server. Minting needs a writable vault credential,
/// which no node holds, so it stays at the desk (<c>RUNBOOK.md</c>).
/// </remarks>
public sealed record RunnerSettings
{
    /// <summary>
    /// The vault the runner secrets live in.
    /// </summary>
    public string? Vault { get; init; }

    /// <summary>
    /// The repository a host runner is scoped to: steps run in a shell on the node, so only the
    /// lab may use it.
    /// </summary>
    public string? Repository { get; init; }

    /// <summary>
    /// The image a containerised runner's jobs run in.
    /// </summary>
    public string? Image { get; init; }
}
