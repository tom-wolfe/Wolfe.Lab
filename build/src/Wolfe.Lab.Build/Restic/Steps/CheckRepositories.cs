using Wolfe.Lab.Build.Restic.Models;

namespace Wolfe.Lab.Build.Restic.Steps;

/// <summary>
/// An unverified backup is a hope. The local check is structural; the offsite one also reads a
/// sample of pack data back, which over a year covers most of the repository for pennies.
/// </summary>
[Step("check repositories", StepKind.Check)]
internal sealed class CheckRepositories(IRestic restic, VerifyOptions options, IWorkflowLog log)
{
    public async Task<StepResult> Run(ResticRepository local, OffsiteRepository offsite, CancellationToken ct = default)
    {
        await restic.Check(local, readDataSubset: null, ct);
        log.Status($"{local.Location} checks out.");
        await restic.Check(offsite.Repository, options.ReadDataSubset, ct);
        log.Status($"{offsite.Repository.Location} checks out, {options.ReadDataSubset} of its data read back.");
        return StepResult.Successful;
    }
}
