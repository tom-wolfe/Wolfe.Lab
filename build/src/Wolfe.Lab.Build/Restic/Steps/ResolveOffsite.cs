using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Restic.Models;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Restic.Steps;

/// <summary>
/// Reads the offsite repository from <c>restic/offsite.env</c>, whose environment names the
/// local repository as the copy's source as well.
/// </summary>
[Step("resolve offsite", StepKind.Work)]
internal sealed class ResolveOffsite(ISecrets secrets, IWorkflowLog log)
{
    internal const string FileName = "offsite.env";
    private const string SourceVariable = "RESTIC_FROM_REPOSITORY";

    public async Task<StepResult<OffsiteRepository>> Run(Slice slice, CancellationToken ct = default)
    {
        var loaded = await ResticEnvironment.Load(slice, FileName, secrets, ct);
        if (loaded.IsError)
        {
            return StepResult.Failed(loaded.Errors!);
        }

        var repository = loaded.Value!;
        if (!repository.Environment.ContainsKey(SourceVariable))
        {
            return new Error($"{FileName} does not set {SourceVariable}: the copy would have no source.");
        }

        log.Detail($"Offsite repository is {repository.Location}.");
        return new OffsiteRepository(repository);
    }
}
