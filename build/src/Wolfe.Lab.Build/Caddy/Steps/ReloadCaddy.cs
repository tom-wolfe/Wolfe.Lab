namespace Wolfe.Lab.Build.Caddy.Steps;

/// <summary>
/// Hands the running front door its new configuration.
/// </summary>
/// <remarks>
/// <c>--force</c> is load-bearing. A plain reload compares the adapted config with the running
/// one and does nothing when they match — and the route snippets and the certificate files are
/// only re-read as part of a real load, so a gather that changed a snippet without changing the
/// Caddyfile would otherwise be a silent no-op.
/// </remarks>
[Step("reload caddy", StepKind.Publish)]
internal sealed class ReloadCaddy(ICommandRunner commands, WorkflowJob job, IWorkflowLog log)
{
    private const string Container = "caddy";
    private const string Config = "/etc/caddy/lab/caddy/Caddyfile";

    public async Task<StepResult> Run(CancellationToken ct = default)
    {
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
}
