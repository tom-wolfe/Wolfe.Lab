using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Restic;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Backup.Steps;

/// <summary>
/// Reads the repository from the restic slice's env file.
/// </summary>
[Step("resolve repository", StepKind.Work)]
internal sealed class ResolveRepository(ISecrets secrets, IWorkflowLog log)
{
    internal const string ConfigFile = "config";

    /// <summary>
    /// Which of the restic slice's env files this node uses.
    /// </summary>
    internal static string FileName => OperatingSystem.IsMacOS() ? "restic.env" : "sftp.env";

    public async Task<StepResult<ResticRepository>> Run(Slice slice, CancellationToken ct = default)
    {
        if (!(await ResticEnvironment.Load(slice, FileName, secrets, ct)).TryGetValue(out var repository, out var errors))
        {
            return StepResult.Failed(errors);
        }

        if (repository.IsLocal && !new PhysicalDirectory(repository.Location).GetFile(ConfigFile).Exists)
        {
            return new Error($"No restic repository at {repository.Location}: the drive is unmounted, or the repository was never initialised (restic/RUNBOOK.md, Bootstrap).");
        }

        log.Detail($"Backing up into {repository.Location}.");
        return repository;
    }
}
