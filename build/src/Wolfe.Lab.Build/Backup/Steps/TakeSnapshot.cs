using Ritten.Docker;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Restic;

namespace Wolfe.Lab.Build.Backup.Steps;

/// <summary>
/// Stop, snapshot, start. The stop is what makes a SQLite or LMDB store consistent on disk.
/// </summary>
[Step("snapshot", StepKind.Work)]
internal sealed class TakeSnapshot(IDocker docker, IRestic restic, BackupPlan plan, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult<Snapshot>> Run(Slice slice, ResticRepository repository, SnapshotImage image, CancellationToken ct = default)
    {
        var tags = new List<string> { $"service:{slice.Name}" };
        if (image.Tag is { } tag)
        {
            tags.Add($"image:{tag}");
        }

        if (plan.Container is not null)
        {
            await docker.ComposeStop(slice.Release, ct);
        }

        try
        {
            var snapshot = await restic.Backup(repository, plan.Paths, plan.Excludes, tags, ct);

            // A deploy converging concurrently would have restarted the stack under the
            // snapshot, leaving live mid-write files in it: discard rather than trust. Not on a
            // rehearsal, where nothing was stopped in the first place.
            if (plan.Container is not null && !job.DryRun && (await docker.Inspect(plan.Container, ct)).Running)
            {
                await restic.Forget(repository, snapshot, ct);
                return new Error($"{plan.Container} was restarted mid-snapshot; snapshot {snapshot.Id} discarded. Run the backup again.");
            }

            if (!job.DryRun)
            {
                log.Status($"Snapshot {snapshot.Id} saved.");
            }

            return snapshot;
        }
        finally
        {
            if (plan.Container is not null)
            {
                // Not the job's token: a cancelled backup still owes the service its restart.
                await docker.ComposeStart(slice.Release, CancellationToken.None);
            }
        }
    }
}
