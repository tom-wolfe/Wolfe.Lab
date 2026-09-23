using Ritten.Engine.FileSystem;

namespace Wolfe.Lab.Build.Clients.Restic.Steps;

/// <summary>
/// Reads the local repository from the restic slice's env file, and checks it is really there.
/// </summary>
[Step("resolve repository", StepKind.Work)]
internal sealed class ResolveRepository(ISecretProvider secrets, IFileSystem fileSystem, IWorkflowLog log)
{
    internal const string ConfigFile = "config";

    /// <summary>
    /// The mini holds the repository on its own drive; a Linux node reaches it over sftp.
    /// </summary>
    internal static string FileName => OperatingSystem.IsMacOS() ? "restic.env" : "sftp.env";

    public async Task<StepResult<ResticRepository>> Run(CancellationToken ct = default)
    {
        if (!(await ResticEnvironment.Load(fileSystem.ProjectRoot, FileName, secrets, ct)).TryGetValue(out var repository, out var errors))
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
