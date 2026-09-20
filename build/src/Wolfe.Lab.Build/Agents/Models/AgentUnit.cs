namespace Wolfe.Lab.Build.Agents.Models;

/// <summary>
/// A rendered unit: what the platform's supervisor reads, and the file it reads it from.
/// </summary>
/// <param name="FileName">The unit's name in the agent directory.</param>
/// <param name="Content">The unit's whole text, which is what a converge compares.</param>
public sealed record AgentUnit(string FileName, string Content);
