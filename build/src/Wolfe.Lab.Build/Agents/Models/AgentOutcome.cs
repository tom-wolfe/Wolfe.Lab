namespace Wolfe.Lab.Build.Agents.Models;

/// <summary>
/// What converging an agent did, or would do.
/// </summary>
public enum AgentOutcome
{
    /// <summary>
    /// The unit on the node already says this, and the supervisor is running it.
    /// </summary>
    Unchanged,

    /// <summary>
    /// The supervisor had never heard of it, and now has.
    /// </summary>
    Installed,

    /// <summary>
    /// The unit changed under a running agent, so the agent was restarted.
    /// </summary>
    Restarted
}
