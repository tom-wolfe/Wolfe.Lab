using Ritten.Docker;
using Wolfe.Lab.Application.Workflows.Backup.Models;
using Wolfe.Lab.Infrastructure.Restic;

namespace Wolfe.Lab.Application.Workflows.Backup.Steps;

/// <summary>
/// Stop, snapshot, start. The stop is what makes a SQLite or LMDB store consistent on disk.
/// </summary>
[Step("snapshot", StepKind.Work)]
internal sealed class TakeSnapshot(IDocker docker, IRestic restic, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult<Snapshot>> Run(BackupPlan plan, ResticRepository repository, SnapshotImage image, CancellationToken ct = default)
    {
        var tags = new List<string> { plan.Tag };
        if (image.Tag is { } tag)
        {
            tags.Add($"image:{tag}");
        }

        if (plan.Stack is { } stopping)
        {
            await docker.ComposeStop(stopping, ct);
        }

        try
        {
            var snapshot = await restic.Backup(repository, plan.Paths, plan.Excludes, tags, ct);

            // A deploy converging concurrently would have restarted the stack under the
            // snapshot, leaving live mid-write files in it: discard rather than trust. Not on a
            // rehearsal, where nothing was stopped in the first place.
            if (plan.Container is not null && !job.DryRun && (await docker.Inspect(plan.Container, ct))?.Running == true)
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
            if (plan.Stack is { } stopped)
            {
                // Not the job's token: a cancelled backup still owes the service its restart.
                await docker.ComposeStart(stopped, CancellationToken.None);
            }
        }
    }
}
