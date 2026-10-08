using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Domain.Secrets;

namespace Wolfe.Lab.Domain.Catalog.Components.Caddy;

/// <summary>
/// lego, as it issues and renews a certificate over ACME with a DNS challenge.
/// </summary>
/// <param name="Image">The lego image it runs.</param>
/// <param name="Email">The ACME account's address.</param>
/// <param name="Dns">The DNS provider the challenge is answered through, as lego names it.</param>
/// <param name="Store">Where lego keeps its account and the certificates, on the node.</param>
public sealed record CertificateIssuer(string Image, string Email, string Dns, HostPath Store)
{
    /// <summary>
    /// The provider's credentials, each a variable lego reads.
    /// </summary>
    public IReadOnlyDictionary<string, SecretReference> Environment { get; init; } = new Dictionary<string, SecretReference>();

    /// <summary>
    /// How long lego waits for the challenge record to propagate, as lego spells it (<c>90s</c>).
    /// </summary>
    public string? PropagationWait { get; init; }
}
