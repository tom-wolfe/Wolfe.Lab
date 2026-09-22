namespace Wolfe.Lab.Build.Agents;

/// <summary>
/// The agents a slice declared, as registration hands them to the steps.
/// </summary>
/// <param name="Agents">The declarations, by the name each was declared under.</param>
public sealed record AgentDeclarations(IReadOnlyDictionary<string, AgentSettings> Agents);
