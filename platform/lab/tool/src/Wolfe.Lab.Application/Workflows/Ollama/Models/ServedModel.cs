using Wolfe.Lab.Domain.Catalog.Components.Models;
using Wolfe.Lab.Infrastructure.Ollama;

namespace Wolfe.Lab.Application.Workflows.Ollama.Models;

/// <summary>
/// Represents a model running on a server.
/// </summary>
/// <param name="Alias">The name callers ask for it by: <c>lab/interactive:latest</c>.</param>
/// <param name="Model">The model the server runs for it.</param>
/// <param name="Context">The context it runs it with, in tokens; the model's own when null.</param>
public sealed record ServedModel(OllamaModel Alias, OllamaModel Model, ContextLength? Context);
