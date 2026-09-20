using Wolfe.Lab.Build.Agents.Models;

namespace Wolfe.Lab.Build.Agents;

/// <summary>
/// The platform's service supervisor — launchd on macOS, systemd user units on Linux.
/// </summary>
public interface IServiceSupervisor
{
    /// <summary>
    /// The unit this agent renders to. Pure: the same definition always renders the same text,
    /// which is what lets an unchanged agent be recognised by comparison alone.
    /// </summary>
    /// <param name="agent">The agent to render.</param>
    AgentUnit Render(AgentDefinition agent);

    /// <summary>
    /// What converging would do, without doing any of it.
    /// </summary>
    /// <param name="agent">The agent to weigh up.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<AgentOutcome> Plan(AgentDefinition agent, CancellationToken ct = default);

    /// <summary>
    /// Makes the supervisor's view match the declaration, and says what that took.
    /// </summary>
    /// <param name="agent">The agent to converge.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<AgentOutcome> Converge(AgentDefinition agent, CancellationToken ct = default);
}
