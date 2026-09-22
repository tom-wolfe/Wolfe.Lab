using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Caddy.Models;

/// <summary>
/// The <c>certificate</c> section of the front door's <c>ritten.json</c>: what lego issues, for
/// whom, through which DNS provider, and where it keeps its state.
/// </summary>
/// <remarks>
/// The provider's credential is an <c>environment</c> entry rather than a named field because
/// lego spells each provider's differently (<c>NETLIFY_TOKEN</c>, and so on); a value may be a
/// reference, read at the moment of use. The image is pinned here like every image in the lab.
/// </remarks>
public sealed record CertificateSettings
{
    /// <summary>
    /// The lego image, tag included.
    /// </summary>
    public string? Image { get; init; }

    /// <summary>
    /// The ACME account's email.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// The names on the certificate; lego files its state under the first.
    /// </summary>
    public IReadOnlyList<string> Domains { get; init; } = [];

    /// <summary>
    /// The lego DNS provider that answers the DNS-01 challenge.
    /// </summary>
    public string? Dns { get; init; }

    /// <summary>
    /// The provider's environment, references included.
    /// </summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// How long to wait for the challenge record to propagate before asking for validation, as
    /// lego spells it (<c>90s</c>).
    /// </summary>
    public string? PropagationWait { get; init; }

    /// <summary>
    /// Where lego keeps the account, the key and the certificates on the node.
    /// </summary>
    public HostPath? Store { get; init; }

    /// <summary>
    /// The request the steps issue, or null while a required field is missing.
    /// </summary>
    public CertificateRequest? ToRequest() =>
        Image is { Length: > 0 } image && Email is { Length: > 0 } email && Domains.Count > 0 && Dns is { Length: > 0 } dns && Store is { } store
            ? new CertificateRequest(image, email, Domains, dns, Environment, PropagationWait, store.Directory)
            : null;
}
