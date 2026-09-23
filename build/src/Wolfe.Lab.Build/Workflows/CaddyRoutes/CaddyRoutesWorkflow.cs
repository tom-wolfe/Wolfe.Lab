using Wolfe.Lab.Build.Workflows.CaddyRoutes.Jobs;

namespace Wolfe.Lab.Build.Workflows.CaddyRoutes;

/// <summary>
/// The front door's routes: <c>"workflow": "caddy-routes"</c>.
/// </summary>
/// <remarks>
/// Every component that wants a hostname drops a <c>caddy.caddyfile</c> beside its compose file;
/// this component is what carries them all to the door. Its own compose component knows nothing
/// of them, which is the point: adding a service never edits the front door.
/// </remarks>
public sealed class CaddyRoutesWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "caddy-routes";

    /// <inheritdoc />
    public string Label => "caddy routes";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new DeployJob()];
}
