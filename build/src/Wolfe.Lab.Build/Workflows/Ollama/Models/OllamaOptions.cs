using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

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
    /// The agents the node keeps running for this slice, by name.
    /// </summary>
    public IReadOnlyDictionary<string, AgentOptions> Agents { get; init; } = new Dictionary<string, AgentOptions>();

    /// <summary>
    /// Where models live and which ones the node should have.
    /// </summary>
    public ModelOptions Models { get; init; } = new();
}
