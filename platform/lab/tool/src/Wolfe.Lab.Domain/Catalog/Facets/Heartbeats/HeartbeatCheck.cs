using Wolfe.Lab.Domain.Secrets;

namespace Wolfe.Lab.Domain.Catalog.Facets.Heartbeats;

/// <summary>
/// The healthchecks.io check a component pings when its work is done: the dead man's switch that
/// pages when it stops.
/// </summary>
/// <param name="Slug">The check.</param>
/// <param name="Key">Where the account's ping key is.</param>
public sealed record HeartbeatCheck(HeartbeatSlug Slug, SecretReference Key);
