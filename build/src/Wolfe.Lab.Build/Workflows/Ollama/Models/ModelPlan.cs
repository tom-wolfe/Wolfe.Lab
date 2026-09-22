namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// The models a deploy will make sure the node has.
/// </summary>
/// <param name="Models">The declared models, in a stable order.</param>
public sealed record ModelPlan(IReadOnlyList<Clients.Ollama.OllamaModel> Models);
