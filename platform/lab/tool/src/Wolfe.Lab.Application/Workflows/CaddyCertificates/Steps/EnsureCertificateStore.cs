using Wolfe.Lab.Domain.Catalog.Components.Caddy;

namespace Wolfe.Lab.Application.Workflows.CaddyCertificates.Steps;

/// <summary>
/// Makes sure lego has somewhere to keep its account, key and certificates.
/// </summary>
[Step("ensure certificate store", StepKind.Work)]
internal sealed class EnsureCertificateStore(WorkflowJob job, IWorkflowLog log)
{
    public StepResult Run(CaddyCertificatesComponent certificate)
    {
        var store = certificate.Issuer.Store.Directory;
        if (store.Exists)
        {
            log.Detail($"{store.AbsolutePath} is there.");
            return StepResult.Successful;
        }

        if (job.DryRun)
        {
            log.Skipped($"Would create {store.AbsolutePath}.");
            return StepResult.Successful;
        }

        store.Create();
        log.Status($"Created {store.AbsolutePath}.");
        return StepResult.Successful;
    }
}
