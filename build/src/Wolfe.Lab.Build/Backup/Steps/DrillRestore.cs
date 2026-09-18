using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Restic;

namespace Wolfe.Lab.Build.Backup.Steps;

/// <summary>
/// A backup nobody has restored from has not shipped. The slice's latest snapshot comes back
/// into a scratch directory, only the paths that prove it, and each is asserted non-empty.
/// </summary>
[Step("drill restore", StepKind.Check)]
internal sealed class DrillRestore(IRestic restic, BackupPlan plan, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(Slice slice, ResticRepository repository, CancellationToken ct = default)
    {
        var tag = $"service:{slice.Name}";
        var snapshot = await restic.FindSnapshot(repository, tag, null, ct);
        if (snapshot is null)
        {
            return new Error($"{repository.Location} holds no snapshot tagged {tag}.");
        }

        var scratch = new PhysicalDirectory(Path.Combine(Path.GetTempPath(), $"lab-drill-{slice.Name}-{Guid.NewGuid():N}"));
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
                if (Describe(Path.Join(scratch.AbsolutePath, path)) is { } size)
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
    private static string? Describe(string restored)
    {
        if (File.Exists(restored))
        {
            var length = new FileInfo(restored).Length;
            return length > 0 ? $"{length / 1024.0 / 1024.0:F1} MiB" : null;
        }

        if (Directory.Exists(restored))
        {
            var entries = Directory.EnumerateFileSystemEntries(restored).Count();
            return entries > 0 ? $"{entries} entr{(entries == 1 ? "y" : "ies")}" : null;
        }

        return null;
    }
}
