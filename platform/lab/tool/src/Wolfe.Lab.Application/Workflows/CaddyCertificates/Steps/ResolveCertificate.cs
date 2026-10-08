using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.CaddyCertificates.Models;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Caddy;

namespace Wolfe.Lab.Application.Workflows.CaddyCertificates.Steps;

/// <summary>
/// The certificate the component declares, or its <c>ritten.json</c> describes while it declares none.
/// </summary>
[Step("resolve certificate", StepKind.Work)]
internal sealed class ResolveCertificate(DeclaredComponents declared, CaddyCertificatesOptions legacy)
{
    public async Task<StepResult<CertificateRequest>> Run(ServiceCatalog catalog, CancellationToken ct = default)
    {
        if (await declared.Find<CaddyCertificatesComponent>(catalog, ct) is { } certificate)
        {
            var issuer = certificate.Issuer;
            return new CertificateRequest(issuer.Image, issuer.Email, certificate.Domains, issuer.Dns,
                issuer.Environment.ToDictionary(variable => variable.Key, variable => variable.Value.Value, StringComparer.Ordinal),
                issuer.PropagationWait, issuer.Store.Directory);
        }

        return legacy.ToRequest() is { } request ? request : new Error("The component declares no certificate.");
    }
}
