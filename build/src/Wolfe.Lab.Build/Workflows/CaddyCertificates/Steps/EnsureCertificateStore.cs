using Wolfe.Lab.Build.Workflows.CaddyCertificates.Models;

namespace Wolfe.Lab.Build.Workflows.CaddyCertificates.Steps;

/// <summary>
/// Makes sure lego has somewhere to keep its account, key and certificates.
/// </summary>
[Step("ensure certificate store", StepKind.Work)]
internal sealed class EnsureCertificateStore(CertificateRequest request, WorkflowJob job, IWorkflowLog log)
{
    public StepResult Run()
    {
        if (request.Store.Exists)
        {
            log.Detail($"{request.Store.AbsolutePath} is there.");
            return StepResult.Successful;
        }

        if (job.DryRun)
        {
            log.Skipped($"Would create {request.Store.AbsolutePath}.");
            return StepResult.Successful;
        }

        request.Store.Create();
        log.Status($"Created {request.Store.AbsolutePath}.");
        return StepResult.Successful;
    }
}
