namespace Wolfe.Lab.Build.Clients.Caddy;

/// <summary>
/// The caddy a step reloads.
/// </summary>
/// <param name="Container">The container caddy runs as.</param>
/// <param name="Caddyfile">The Caddyfile's path inside it.</param>
public sealed record CaddyInstance(string Container, string Caddyfile);
