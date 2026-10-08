using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>caddy-certificates</c> workflow operates, as its file writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record CaddyCertificatesDocument : ComponentDocument
{
    [Required, MinLength(1), UniqueItems(true), Description("The names the certificate covers: *.twolfe.dev.")]
    public List<string> Domains { get; init; } = [];

    [Required, Description("What issues and renews it.")]
    public IssuerDocument Issuer { get; init; } = new();
}
