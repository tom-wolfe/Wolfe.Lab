using Wolfe.Lab.Build.Clients.Ollama;

namespace Wolfe.Lab.Build.Workflows.Ollama.Steps;

/// <summary>
/// Waits for the server the converge just started, or restarted, to answer.
/// </summary>
[Step("await server", StepKind.Work)]
internal sealed class AwaitServer(IOllama ollama, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        if (!await ollama.AwaitServing(ct))
        {
            return new Error("The server did not answer after being converged: see its log.");
        }

        log.Detail("The server is answering.");
        return StepResult.Successful;
    }
}
