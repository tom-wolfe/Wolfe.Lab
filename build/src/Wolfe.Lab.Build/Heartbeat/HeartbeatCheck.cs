using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Heartbeat;

/// <summary>
/// A healthchecks.io check: the dead man's switch that pages when a job stops running.
/// </summary>
/// <param name="Slug">The check's slug.</param>
/// <param name="Key">Where the account's ping key is.</param>
public sealed record HeartbeatCheck(string Slug, SecretReference Key);
