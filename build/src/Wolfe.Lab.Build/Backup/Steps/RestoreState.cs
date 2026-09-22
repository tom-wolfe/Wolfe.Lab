using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Backup.Steps;

/// <summary>
/// Stop, set the live state aside, restore to where it was, start. The rename is the undo:
/// nothing is deleted, and a restore that fails puts the live state back before the stack
/// comes up, so the service never boots onto a half-written directory.
/// </summary>
[Step("restore", StepKind.Publish)]
internal sealed class RestoreState(IDocker docker, IRestic restic, IStateDirectories directories, BackupPlan plan, IWorkflowLog log)
{
    internal const string Root = "/";

    public async Task<StepResult> Run(Slice slice, ResticRepository repository, RestorePoint point, CancellationToken ct = default)
    {
        var suffix = $".bak-{DateTimeOffset.Now:yyyyMMdd-HHmmss}";
        if (plan.Container is not null)
        {
            await docker.ComposeStop(slice.Release, ct);
        }

        try
        {
            var aside = new List<(IDirectory Live, IDirectory Aside)>();
            foreach (var live in plan.Paths)
            {
                var moved = new PhysicalDirectory(live.AbsolutePath + suffix);
                if (directories.Move(live, moved))
                {
                    aside.Add((live, moved));
                    log.Detail($"{live.AbsolutePath} set aside as {moved.AbsolutePath}.");
                }
            }

            try
            {
                await restic.Restore(repository, point.Snapshot.Id, new PhysicalDirectory(Root), [], ct);
            }
            catch
            {
                foreach (var (live, moved) in aside)
                {
                    directories.Move(moved, live);
                }

                log.Warning("The restore failed; the live state is back where it was.");
                throw;
            }

            log.Status($"Restored snapshot {point.Snapshot.Id}. The previous state is beside it with the {suffix} suffix; delete it once the service has proved itself.");
            return StepResult.Successful;
        }
        finally
        {
            if (plan.Container is not null)
            {
                await docker.ComposeStart(slice.Release, CancellationToken.None);
            }
        }
    }
}
