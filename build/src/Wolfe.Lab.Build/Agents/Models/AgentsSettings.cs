using Wolfe.Lab.Build.Deploy.Models;

namespace Wolfe.Lab.Build.Agents.Models;

/// <summary>
/// The shape of an agent-owning slice's <c>ritten.json</c>.
/// </summary>
public record AgentsSettings : SliceSettings
{
    /// <summary>
    /// The agents this slice wants running, by name. The name becomes the label, so
    /// <c>ollama</c> is <c>dev.twolfe.ollama</c>.
    /// </summary>
    public IReadOnlyDictionary<string, AgentSettings> Agents { get; init; } = new Dictionary<string, AgentSettings>();
}
