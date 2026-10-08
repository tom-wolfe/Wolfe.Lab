using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Caddy;

namespace Wolfe.Lab.Application.Workflows.CaddyCertificates.Steps;

/// <summary>
/// The certificate the component declares.
/// </summary>
[Step("resolve certificate", StepKind.Work)]
internal sealed class ResolveCertificate(IWorkflowLog log)
{
    public StepResult<CaddyCertificatesComponent> Run(DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(WorkflowName.CaddyCertificates).TryGetValue(out var declared, out var errors))
        {
            return StepResult.Failed(errors);
        }

        var certificate = (CaddyCertificatesComponent)declared;
        log.Detail($"Covers {string.Join(", ", certificate.Domains)}, issued by {certificate.Issuer.Image} through {certificate.Issuer.Dns}.");
        return certificate;
    }
}
