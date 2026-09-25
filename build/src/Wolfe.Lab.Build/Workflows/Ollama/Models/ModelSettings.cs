using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// The <c>models</c> section of <c>ollama/ritten.json</c>: where models live, which ones the
/// node should have, and which of them fills each role.
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

    /// <summary>
    /// The roles the node fills, by name. A caller asks for <c>lab/&lt;role&gt;</c> and gets
    /// this node's model for it, so it never has to know which machine answered.
    /// </summary>
    public IReadOnlyDictionary<string, ModelRole> Roles { get; init; } = new Dictionary<string, ModelRole>();
}
