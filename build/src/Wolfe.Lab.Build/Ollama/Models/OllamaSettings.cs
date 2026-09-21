using Wolfe.Lab.Build.Agents.Models;

namespace Wolfe.Lab.Build.Ollama.Models;

/// <summary>
/// The shape of <c>ollama/ritten.json</c>.
/// </summary>
public sealed record OllamaSettings : AgentsSettings
{
    /// <summary>
    /// Where models live and which ones the node should have.
    /// </summary>
    public ModelSettings Models { get; init; } = new();
}
