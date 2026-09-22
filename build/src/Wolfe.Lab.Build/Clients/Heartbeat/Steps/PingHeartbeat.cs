namespace Wolfe.Lab.Build.Clients.Heartbeat.Steps;

/// <summary>
/// The last step of a job a dead man's switch watches: reached only when everything before it
/// succeeded, so the ping is the job's own word that it ran.
/// </summary>
[Step("heartbeat", StepKind.Work)]
internal sealed class PingHeartbeat(IHeartbeat heartbeat, HeartbeatCheck check, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        await heartbeat.Ping(check, ct);
        if (!job.DryRun)
        {
            log.Detail($"Pinged {check.Slug}.");
        }

        return StepResult.Successful;
    }
}
