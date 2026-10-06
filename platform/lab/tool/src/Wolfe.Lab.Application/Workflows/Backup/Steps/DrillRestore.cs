using Wolfe.Lab.Application.Workflows.Backup.Models;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Infrastructure.Restic;

namespace Wolfe.Lab.Application.Workflows.Backup.Steps;

/// <summary>
/// A backup nobody has restored from has not shipped. The service's latest snapshot comes back
/// into a scratch directory, only the paths that prove it, and each is asserted non-empty.
/// </summary>
[Step("drill restore", StepKind.Check)]
internal sealed class DrillRestore(IRestic restic, BackupPlan plan, IFileSystem fileSystem, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(Release release, ResticRepository repository, CancellationToken ct = default)
    {
        var tag = $"service:{release.Name}";
        var snapshot = await restic.FindSnapshot(repository, tag, null, ct);
        if (snapshot is null)
        {
            return new Error($"{repository.Location} holds no snapshot tagged {tag}.");
        }

        var scratch = fileSystem.CreateTempDirectory($"lab-drill-{release.Name}-");
        try
        {
            await restic.Restore(repository, snapshot.Id, scratch, plan.Verify, ct);
            if (job.DryRun)
            {
                log.Skipped($"Would assert {plan.Verify.Count} path{(plan.Verify.Count == 1 ? "" : "s")} from snapshot {snapshot.Id}.");
                return StepResult.Successful;
            }

            var errors = new List<Error>();
            foreach (var path in plan.Verify)
            {
                if (Describe(scratch, path) is { } size)
                {
                    log.Detail($"{path} came back from {snapshot.Id} ({size}).");
                }
                else
                {
                    errors.Add(new Error($"{path} is missing or empty in snapshot {snapshot.Id}."));
                }
            }

            if (errors.Count > 0)
            {
                return StepResult.Failed(errors);
            }

            log.Status($"Snapshot {snapshot.Id} from {snapshot.Time:yyyy-MM-dd HH:mm} restores.");
            return StepResult.Successful;
        }
        finally
        {
            if (scratch.Exists)
            {
                scratch.Delete();
            }
        }
    }

    /// <summary>
    /// What came back, or null for nothing: a non-empty file's size, or a directory with contents.
    /// </summary>
    /// <remarks>
    /// A path is the node's, absolute, and restic restores it beneath the scratch as written.
    /// </remarks>
    private static string? Describe(IDirectory scratch, string path)
    {
        var restored = path.TrimStart('/');
        if (scratch.GetFile(restored) is { Exists: true } file)
        {
            using var stream = file.OpenRead();
            var length = stream.Length;
            return length > 0 ? $"{length / 1024.0 / 1024.0:F1} MiB" : null;
        }

        if (scratch.GetDirectory(restored) is { Exists: true } directory)
        {
            var entries = directory.GetFiles().Count() + directory.GetDirectories().Count();
            return entries > 0 ? $"{entries} entr{(entries == 1 ? "y" : "ies")}" : null;
        }

        return null;
    }
}
