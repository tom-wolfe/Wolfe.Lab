namespace Wolfe.Lab.Build.Clients.Caddy;

/// <summary>
/// The running caddy a component reloads: the <c>caddy</c> block of its <c>ritten.json</c>.
/// </summary>
/// <remarks>
/// Facts of <c>caddy/compose</c> — the container it names, the path it mounts the Caddyfile at —
/// that every component reloading caddy repeats. The first candidate for the config plane
/// (ROADMAP.md #8): a value one component owns and others read.
/// </remarks>
public sealed record CaddyOptions
{
    /// <summary>
    /// The container caddy runs as.
    /// </summary>
    public string? Container { get; init; }

    /// <summary>
    /// The Caddyfile's path inside that container.
    /// </summary>
    public string? Caddyfile { get; init; }

    /// <summary>
    /// The instance, or null while either is missing.
    /// </summary>
    public CaddyInstance? ToInstance() =>
        Container is { Length: > 0 } && Caddyfile is { Length: > 0 } ? new CaddyInstance(Container, Caddyfile) : null;
}
