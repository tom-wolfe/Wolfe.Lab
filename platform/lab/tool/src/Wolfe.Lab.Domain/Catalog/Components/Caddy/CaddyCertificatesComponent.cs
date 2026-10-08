namespace Wolfe.Lab.Domain.Catalog.Components.Caddy;

/// <summary>
/// A component the <c>caddy-certificates</c> workflow operates: a certificate Caddy serves, kept renewed.
/// </summary>
public sealed class CaddyCertificatesComponent : Component
{
    private CaddyCertificatesComponent() { }

    /// <summary>
    /// The names it covers: <c>*.twolfe.dev</c>.
    /// </summary>
    public required IReadOnlyList<string> Domains { get; init; }

    /// <summary>
    /// What issues and renews it.
    /// </summary>
    public required CertificateIssuer Issuer { get; init; }

    /// <summary>
    /// Creates a new certificate component.
    /// </summary>
    public static Result<CaddyCertificatesComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, IReadOnlyList<string> domains, CertificateIssuer issuer)
    {
        var errors = Validate(source, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new CaddyCertificatesComponent
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = WorkflowName.CaddyCertificates,
            Domains = domains,
            Issuer = issuer
        };
    }
}
