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

/// <summary>
/// The model store as the steps consume it.
/// </summary>
/// <param name="Directory">The directory ollama keeps models in.</param>
public sealed record ModelStore(IDirectory Directory);

/// <summary>
/// The models a deploy will make sure the node has.
/// </summary>
/// <param name="Models">The declared models, in a stable order.</param>
public sealed record ModelPlan(IReadOnlyList<OllamaModel> Models);
