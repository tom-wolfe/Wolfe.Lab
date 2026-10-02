namespace Wolfe.Lab.Infrastructure.Agents;

/// <summary>
/// The agents a service declared, as registration hands them to the steps.
/// </summary>
/// <param name="Agents">The declarations, by the name each was declared under.</param>
public sealed record AgentDeclarations(IReadOnlyDictionary<string, AgentOptions> Agents);
