using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Caddy.Models;

/// <summary>
/// The front door's <c>ritten.json</c>: what every slice declares, plus the certificate it serves.
/// </summary>
public sealed record CaddySettings : SliceSettings
{
    /// <summary>
    /// The wildcard certificate lego issues and renews.
    /// </summary>
    public CertificateSettings Certificate { get; init; } = new();
}
