namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// The model store as the steps consume it.
/// </summary>
/// <param name="Directory">The directory ollama keeps models in.</param>
public sealed record ModelStore(IDirectory Directory);
