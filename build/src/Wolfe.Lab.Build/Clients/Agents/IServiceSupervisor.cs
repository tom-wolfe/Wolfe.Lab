

namespace Wolfe.Lab.Build.Clients.Agents;

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

    /// <summary>
    /// Whether the supervisor has a unit by this name, loaded or only on disk.
    /// </summary>
    /// <param name="unit">The unit's name as its supervisor knows it.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<bool> IsInstalled(string unit, CancellationToken ct = default);

    /// <summary>
    /// Stops a unit and removes it, so it does not start again at the next boot or login. Does
    /// nothing when there is no such unit.
    /// </summary>
    /// <param name="unit">The unit's name as its supervisor knows it.</param>
    /// <param name="ct">The cancellation token.</param>
    Task Retire(string unit, CancellationToken ct = default);
}
