using Wolfe.Lab.Build.Ollama.Models;

namespace Wolfe.Lab.Build.Ollama.Steps;

/// <summary>
/// Creates the model directory before the agent is told to use it.
/// </summary>
/// <remarks>
/// A container gets its bind-mounted state directory made for it by the runtime; a host
/// process gets nothing, and ollama did not create the OLLAMA_MODELS it was handed. Declaring
/// the directory and making it is the slice's job, the same way a compose slice declares the
/// volume it binds.
/// </remarks>
[Step("ensure model store", StepKind.Work)]
internal sealed class EnsureModelStore(ModelStore store, IWorkflowLog log)
{
    public StepResult Run()
    {
        if (store.Directory.Exists)
        {
            log.Detail($"{store.Directory.AbsolutePath} is there.");
            return StepResult.Successful;
        }

        store.Directory.Create();
        log.Status($"Created {store.Directory.AbsolutePath}.");
        return StepResult.Successful;
    }
}
