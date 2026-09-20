using Wolfe.Lab.Build.Ollama.Jobs;

namespace Wolfe.Lab.Build.Ollama.Workflows;

/// <summary>
/// The model server.
/// </summary>
public sealed class OllamaWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "ollama";

    /// <inheritdoc />
    public string Label => "ollama";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new DeployJob()];
}
