using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Application.Workflows.Ollama.Models;

/// <summary>
/// The shape of <c>ai/ollama/ritten.json</c>.
/// </summary>
public sealed record OllamaOptions : WorkflowSettings
{
    /// <summary>
    /// External volumes the model store lives on.
    /// </summary>
    public IReadOnlyList<HostPath> Volumes { get; init; } = [];


    /// <summary>
    /// Where models live and which ones the node should have.
    /// </summary>
    public ModelOptions Models { get; init; } = new();
}
