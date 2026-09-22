using Wolfe.Lab.Build.Agents;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// The shape of <c>ollama/ritten.json</c>.
/// </summary>
public sealed record OllamaSettings : SliceSettings
{
    /// <summary>
    /// The agents the node keeps running for this slice, by name.
    /// </summary>
    public IReadOnlyDictionary<string, AgentSettings> Agents { get; init; } = new Dictionary<string, AgentSettings>();

    /// <summary>
    /// Where models live and which ones the node should have.
    /// </summary>
    public ModelSettings Models { get; init; } = new();
}
