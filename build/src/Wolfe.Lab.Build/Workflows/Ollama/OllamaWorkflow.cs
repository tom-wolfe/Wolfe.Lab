
using Wolfe.Lab.Build.Workflows.Ollama.Jobs;

namespace Wolfe.Lab.Build.Workflows.Ollama;

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
    public IReadOnlyList<IJob> Jobs { get; } = [new CheckJob(), new DeployJob()];
}
