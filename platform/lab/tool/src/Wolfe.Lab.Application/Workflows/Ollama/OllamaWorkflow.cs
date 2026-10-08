using Wolfe.Lab.Application.Workflows.Ollama.Jobs;

namespace Wolfe.Lab.Application.Workflows.Ollama;

/// <summary>
/// The models the lab's servers serve, by what each is used for: <c>"workflow": "ollama"</c>.
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
