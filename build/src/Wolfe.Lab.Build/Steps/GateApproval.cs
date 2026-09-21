namespace Wolfe.Lab.Build.Steps;

/// <summary>
/// Stops and asks before a job publishes.
/// </summary>
[Step("approval gate", StepKind.Gate)]
internal sealed class GateApproval(WorkflowJob job, IWorkflowLog log, IWorkflowPrompt prompt)
{
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (job.DryRun)
        {
            log.Skipped("Nothing to approve: this is a dry run.");
            return StepResult.Successful;
        }

        if (job.AutoApprove)
        {
            log.Skipped($"Approved automatically by --{WorkflowArguments.AutoApprove}.");
            return StepResult.Successful;
        }

        if (!prompt.IsInteractive)
        {
            // Hanging on a runner waiting for a person is worse than refusing to start.
            return StepResult.Failed(
                $"The {job.Name} job needs approval, and there's no terminal to ask at. " +
                $"Pass --{WorkflowArguments.AutoApprove} to approve it up front.");
        }

        if (!await prompt.Confirm($"{job.Workflow} {job.Name} is about to publish.", cancellationToken))
        {
            return StepResult.Failed($"The {job.Name} job was not approved.");
        }

        return StepResult.Successful;
    }
}
