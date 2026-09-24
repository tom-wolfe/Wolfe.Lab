using System.Diagnostics;
using Wolfe.Lab.Build.Clients.Ollama;
using Wolfe.Lab.Build.Workflows.Ollama.Models;

namespace Wolfe.Lab.Build.Workflows.Ollama.Steps;

/// <summary>
/// Waits for the server the converge just started, or restarted, to answer.
/// </summary>
[Step("await server", StepKind.Work)]
internal sealed class AwaitServer(IOllama ollama, ServerWait wait, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        if (job.DryRun)
        {
            log.Skipped("Would wait for the server to answer.");
            return StepResult.Successful;
        }

        var clock = Stopwatch.StartNew();
        while (true)
        {
            if (await ollama.IsServing(ct))
            {
                log.Detail($"The server answered after {clock.Elapsed.TotalSeconds:0.#}s.");
                return StepResult.Successful;
            }

            if (clock.Elapsed >= wait.Timeout)
            {
                return new Error($"The server did not answer within {wait.Timeout.TotalSeconds:0}s of being converged: see its log.");
            }

            await Task.Delay(wait.Interval, ct);
        }
    }
}
