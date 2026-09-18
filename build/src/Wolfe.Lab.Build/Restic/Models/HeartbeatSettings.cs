using Wolfe.Lab.Build.Heartbeat;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Restic.Models;

/// <summary>
/// The healthchecks.io check that hears about a green offsite copy.
/// </summary>
public sealed record HeartbeatSettings
{
    /// <summary>
    /// The check's slug.
    /// </summary>
    public string? Check { get; init; }

    /// <summary>
    /// Where the account's ping key is.
    /// </summary>
    public SecretReference? Key { get; init; }

    /// <summary>
    /// The check as the steps consume it, or null while either half is missing — the one
    /// question validation asks and registration answers.
    /// </summary>
    public HeartbeatCheck? ToCheck() => Check is { Length: > 0 } slug && Key is { } key ? new HeartbeatCheck(slug, key) : null;
}
