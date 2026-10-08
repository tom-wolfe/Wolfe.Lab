using Wolfe.Lab.Domain.Catalog.Facets.Heartbeats;
using Wolfe.Lab.Domain.Secrets;

namespace Wolfe.Lab.Infrastructure.Heartbeat;

/// <summary>
/// A <c>ritten.json</c>'s healthchecks.io check, read until the component declares its <c>heartbeat</c>.
/// </summary>
public sealed record HeartbeatCheckOptions
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
    public HeartbeatCheck? ToCheck() => Check != null && HeartbeatSlug.TryFrom(Check) is { IsSuccess: true } valid && Key is { } key ? new HeartbeatCheck(valid.ValueObject, key) : null;
}
