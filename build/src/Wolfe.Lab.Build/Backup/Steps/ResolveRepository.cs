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
    internal const string SliceName = "restic";
    internal const string ConfigFile = "config";

    /// <summary>
    /// Which of the restic slice's env files this node uses.
    /// </summary>
    internal static string FileName => OperatingSystem.IsMacOS() ? "restic.env" : "sftp.env";

    public async Task<StepResult<ResticRepository>> Run(Slice slice, CancellationToken ct = default)
    {
        // Slices are siblings in the checkout; the restic slice is the one with the env files.
        var file = new PhysicalDirectory(Path.Combine(slice.Source.AbsolutePath, "..", SliceName)).GetFile(FileName);
        if (!file.Exists)
        {
            return new Error($"{file.AbsolutePath} does not exist: the checkout has no restic slice beside {slice.Name}.");
        }

        using var reader = new StreamReader(file.OpenRead());
        var entries = EnvFile.Parse(await reader.ReadToEndAsync(ct), file.AbsolutePath);
        if (entries.IsError)
        {
            return StepResult.Failed(entries.Errors!);
        }

        if (!entries.Value!.ContainsKey(ResticRepository.LocationVariable))
        {
            return new Error($"{file.AbsolutePath} does not set {ResticRepository.LocationVariable}.");
        }

        var repository = new ResticRepository(await entries.Value.Resolve(secrets, ct));
        if (repository.IsLocal && !new PhysicalDirectory(repository.Location).GetFile(ConfigFile).Exists)
        {
            return new Error($"No restic repository at {repository.Location}: the drive is unmounted, or the repository was never initialised (restic/RUNBOOK.md, Bootstrap).");
        }

        log.Detail($"Backing up into {repository.Location}.");
        return repository;
    }
}
