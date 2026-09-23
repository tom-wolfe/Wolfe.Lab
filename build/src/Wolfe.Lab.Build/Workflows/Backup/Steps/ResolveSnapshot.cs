using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Workflows.Backup.Models;

namespace Wolfe.Lab.Build.Workflows.Backup.Steps;

/// <summary>
/// Finds the snapshot to bring back: the one asked for, or the slice's latest.
/// </summary>
[Step("resolve snapshot", StepKind.Work)]
internal sealed class ResolveSnapshot(IRestic restic, RestoreRequest request, IWorkflowLog log)
{
    public async Task<StepResult<RestorePoint>> Run(Release release, ResticRepository repository, CancellationToken ct = default)
    {
        var tag = $"service:{release.Name}";
        var snapshot = await restic.FindSnapshot(repository, tag, request.SnapshotId, ct);
        if (snapshot is null)
        {
            return new Error(request.SnapshotId is null
                ? $"{repository.Location} holds no snapshot tagged {tag}."
                : $"{repository.Location} holds no snapshot {request.SnapshotId} tagged {tag}.");
        }

        log.Detail($"Snapshot {snapshot.Id} from {snapshot.Time:yyyy-MM-dd HH:mm}{(snapshot.Image is { } image ? $", taken under {image}" : "")}.");
        return new RestorePoint(snapshot);
    }
}
