using Ritten.Docker;

using Wolfe.Lab.Infrastructure.Caddy;

namespace Wolfe.Lab.Application.Caddy;

/// <summary>
/// Hands the running caddy its files again.
/// </summary>
/// <remarks>
/// <c>--force</c> is load-bearing: a plain reload compares the adapted config with the running
/// one and does nothing when they match, and certificate files are only re-read as part of a
/// real load. A renewal changes the files and never the Caddyfile, so without it caddy served
/// the old certificate through "successful" reloads. A caddy that is not running has nothing to
/// reload and picks the files up at its first start, which is the bootstrap order.
/// </remarks>
[Step("reload caddy", StepKind.Publish)]
internal sealed class ReloadCaddy(CaddyInstance caddy, IDocker docker, WorkflowJob job, IWorkflowLog log)
{

    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        if (!await IsRunning(ct))
        {
            log.Status($"{caddy.Container} is not running; nothing to reload.");
            return StepResult.Successful;
        }

        if (job.DryRun)
        {
            log.Skipped($"Would reload {caddy.Container} from {caddy.Caddyfile}.");
            return StepResult.Successful;
        }

        await docker.Exec(new ContainerExec(caddy.Container, ["caddy", "reload", "--config", caddy.Caddyfile, "--force"]), ct);

        log.Status("Reloaded caddy.");
        return StepResult.Successful;
    }

    // No such container is never started, or removed: either way, nothing to reload.
    private async Task<bool> IsRunning(CancellationToken ct) => (await docker.Inspect(caddy.Container, ct))?.Running == true;
}
