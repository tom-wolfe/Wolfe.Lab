namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// This component's <c>models</c> section, as the role check judges it.
/// </summary>
/// <param name="Models">The section.</param>
public sealed record DeclaredRoles(ModelSettings Models);
