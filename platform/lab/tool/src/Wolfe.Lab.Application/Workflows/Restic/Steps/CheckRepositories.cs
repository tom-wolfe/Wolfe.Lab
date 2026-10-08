using Wolfe.Lab.Domain.Catalog.Components.Restic;
using Wolfe.Lab.Infrastructure.Restic;

namespace Wolfe.Lab.Application.Workflows.Restic.Steps;

/// <summary>
/// An unverified backup is a hope. The local check is structural; the offsite one also reads a
/// sample of pack data back, which over a year covers most of the repository for pennies.
/// </summary>
[Step("check repositories", StepKind.Check)]
internal sealed class CheckRepositories(IRestic restic, IWorkflowLog log)
{
    public async Task<StepResult> Run(ResticComponent repositories, ResticRepository local, OffsiteRepository offsite, CancellationToken ct = default)
    {
        await restic.Check(local, readDataSubset: null, ct);
        log.Status($"{local.Location} checks out.");
        await restic.Check(offsite.Repository, repositories.VerifySample, ct);
        log.Status($"{offsite.Repository.Location} checks out, {repositories.VerifySample?.Value}% of its data read back.");
        return StepResult.Successful;
    }
}
