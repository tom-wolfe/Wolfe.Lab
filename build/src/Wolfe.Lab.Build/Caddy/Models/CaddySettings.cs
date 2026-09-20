using Wolfe.Lab.Build.Deploy.Models;

namespace Wolfe.Lab.Build.Caddy.Models;

/// <summary>
/// The shape of <c>caddy/ritten.json</c>: <c>"workflow": "caddy"</c>. Nothing beyond what every
/// slice declares — the front door's settings are its Caddyfile, not its job's.
/// </summary>
public sealed record CaddySettings : SliceSettings;
