using Ritten.Docker;

namespace Wolfe.Lab.Build.Workflows.Caddy.Steps;

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
internal sealed class ReloadCaddy(IDocker docker, ICommandRunner commands, WorkflowJob job, IWorkflowLog log)
{
    private const string Container = "caddy";
    private const string Config = "/etc/caddy/lab/caddy/Caddyfile";

    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        if (!await IsRunning(ct))
        {
            log.Status($"{Container} is not running; nothing to reload.");
            return StepResult.Successful;
        }

        if (job.DryRun)
        {
            log.Skipped($"Would reload {Container} from {Config}.");
            return StepResult.Successful;
        }

        await commands.Run(
            Command.Create("docker")
                .WithArguments("exec", Container, "caddy", "reload", "--config", Config, "--force")
                .ThrowOnError(),
            ct);

        log.Status("Reloaded caddy.");
        return StepResult.Successful;
    }

    private async Task<bool> IsRunning(CancellationToken ct)
    {
        try
        {
            return (await docker.Inspect(Container, ct)).Running;
        }
        catch (CommandFailedException)
        {
            // No such container: never started, or removed. Either way there is nothing to reload.
            return false;
        }
    }
}
