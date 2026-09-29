using Wolfe.Lab.Build.Clients.Caddy;
namespace Wolfe.Lab.Build.Workflows.CaddyRoutes.Models;

/// <summary>
/// The shape of the routes component's <c>ritten.json</c>: <c>"workflow": "caddy-routes"</c>.
/// </summary>
public sealed record CaddyRoutesOptions : WorkflowSettings
{
    /// <summary>
    /// The name the gathered routes are installed under: the directory the Caddyfile's import
    /// glob reads, so the two must agree.
    /// </summary>
    public string? Release { get; init; }

    /// <summary>
    /// The caddy this component reloads.
    /// </summary>
    public CaddyOptions Caddy { get; init; } = new();
}
