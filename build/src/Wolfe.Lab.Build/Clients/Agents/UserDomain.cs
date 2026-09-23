namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// The launchd domain the lab's agents live in — <c>gui/501</c>. A user agent, never a system
/// daemon: the GUI domain is the one a job already running as the user can bootstrap into
/// without asking anyone for a password.
/// </summary>
/// <param name="Value">The domain target.</param>
public sealed record UserDomain(string Value)
{
    /// <summary>
    /// The domain for a user id.
    /// </summary>
    /// <param name="userId">The id the node's session belongs to.</param>
    public static UserDomain ForUser(string userId) => new($"gui/{userId.Trim()}");

    /// <summary>
    /// A service target in this domain.
    /// </summary>
    /// <param name="label">The agent's label.</param>
    public string Target(AgentLabel label) => $"{Value}/{label.Value}";
}
