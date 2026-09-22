using Wolfe.Lab.Build.Clients.Ollama;
using Wolfe.Lab.Build.Workflows.Ollama.Models;

namespace Wolfe.Lab.Build.Workflows.Ollama.Steps;

/// <summary>
/// Fetches the declared models the node does not have.
/// </summary>
[Step("pull models", StepKind.Publish)]
internal sealed class PullModels(IOllama ollama, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(ModelPlan missing, CancellationToken ct = default)
    {
        foreach (var model in missing.Models)
        {
            await ollama.Pull(model, ct);
            if (!job.DryRun)
            {
                log.Status($"Pulled {model.Value}.");
            }
        }

        return StepResult.Successful;
    }
}
