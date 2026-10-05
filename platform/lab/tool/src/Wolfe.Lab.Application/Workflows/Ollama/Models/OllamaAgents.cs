using Wolfe.Lab.Infrastructure.Agents;

namespace Wolfe.Lab.Application.Workflows.Ollama.Models;

/// <summary>
/// The agents the component's <c>ritten.json</c> declares.
/// </summary>
/// <param name="Agents">The agents, by name.</param>
internal sealed record OllamaAgents(IReadOnlyDictionary<string, AgentOptions> Agents);
