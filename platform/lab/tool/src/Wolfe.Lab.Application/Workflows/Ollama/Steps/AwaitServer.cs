using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Infrastructure.Ollama;

namespace Wolfe.Lab.Application.Workflows.Ollama.Steps;

/// <summary>
/// Waits for the server the converge just started, or restarted, to answer.
/// </summary>
[Step("await server", StepKind.Work)]
internal sealed class AwaitServer(IOllama ollama, IWorkflowLog log)
{
    public async Task<StepResult> Run(ServerPlan plan, CancellationToken ct = default)
    {
        if (plan.Server is null)
        {
            return StepResult.Successful;
        }

        if (!await ollama.AwaitServing(ct))
        {
            return new Error("The server did not answer after being converged: see its log.");
        }

        log.Detail("The server is answering.");
        return StepResult.Successful;
    }
}
