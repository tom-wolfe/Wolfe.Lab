using Wolfe.Lab.Infrastructure.Caddy;

namespace Wolfe.Lab.Application.Workflows.CaddyRoutes.Models;

/// <summary>
/// The shape of the routes component's <c>ritten.json</c>: <c>"workflow": "caddy-routes"</c>.
/// </summary>
public sealed record CaddyRoutesOptions : WorkflowSettings
{
    /// <summary>
    /// The caddy this component reloads.
    /// </summary>
    public CaddyOptions Caddy { get; init; } = new();
}
