using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Slices;
using Wolfe.Lab.Build.Workflows.Restic.Models;

namespace Wolfe.Lab.Build.Workflows.Restic.Steps;

/// <summary>
/// Reads the offsite repository from <c>restic/offsite.env</c>, whose environment names the
/// local repository as the copy's source as well.
/// </summary>
[Step("resolve offsite", StepKind.Work)]
internal sealed class ResolveOffsite(ISecretProvider secrets, IWorkflowLog log)
{
    internal const string FileName = "offsite.env";
    private const string SourceVariable = "RESTIC_FROM_REPOSITORY";

    public async Task<StepResult<OffsiteRepository>> Run(Slice slice, CancellationToken ct = default)
    {
        if (!(await ResticEnvironment.Load(slice, FileName, secrets, ct)).TryGetValue(out var repository, out var errors))
        {
            return StepResult.Failed(errors);
        }

        if (!repository.Environment.ContainsKey(SourceVariable))
        {
            return new Error($"{FileName} does not set {SourceVariable}: the copy would have no source.");
        }

        log.Detail($"Offsite repository is {repository.Location}.");
        return new OffsiteRepository(repository);
    }
}
