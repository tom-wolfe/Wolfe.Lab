using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// The <c>models</c> section of <c>ollama/ritten.json</c>: where models live, and which ones
/// the node should have.
/// </summary>
public sealed record ModelSettings
{
    /// <summary>
    /// The directory ollama keeps models in.
    /// </summary>
    public HostPath? Store { get; init; }

    /// <summary>
    /// The models the node should have. Pulled if missing, left alone if present.
    /// </summary>
    public IReadOnlyList<Clients.Ollama.OllamaModel> Pull { get; init; } = [];
}
