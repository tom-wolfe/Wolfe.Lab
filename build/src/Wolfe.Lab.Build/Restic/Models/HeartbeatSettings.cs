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
}