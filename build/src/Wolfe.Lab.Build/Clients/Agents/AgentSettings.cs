using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// One entry of the <c>agents</c> section of a slice's <c>ritten.json</c>.
/// </summary>
public sealed record AgentSettings
{
    /// <summary>
    /// The executable the agent runs.
    /// </summary>
    public HostPath? Program { get; init; }

    /// <summary>
    /// The arguments it runs with.
    /// </summary>
    public IReadOnlyList<string> Arguments { get; init; } = [];

    /// <summary>
    /// Environment variables set for the process.
    /// </summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// The directory it runs from, when it cares.
    /// </summary>
    public HostPath? WorkingDirectory { get; init; }

    /// <summary>
    /// Where its output goes. Both streams land in the one file, the way a service's log reads.
    /// </summary>
    public HostPath? Log { get; init; }

    /// <summary>
    /// Whether the supervisor starts it again when it exits. An agent worth declaring usually
    /// wants this, so it is the default.
    /// </summary>
    public bool KeepAlive { get; init; } = true;

    /// <summary>
    /// How long it gets to stop before it is killed, for an agent that should finish what it
    /// is doing.
    /// </summary>
    public int? ExitTimeout { get; init; }

    /// <summary>
    /// How a changed unit reaches the running process.
    /// </summary>
    public AgentRestart Restart { get; init; } = AgentRestart.Reload;

    /// <summary>
    /// Units an earlier supervisor ran this agent under — Homebrew's <c>sh.brew.beszel-agent</c>,
    /// a hand-written systemd unit — which the converge stops and removes before it starts this
    /// one, so two copies of one agent never run side by side.
    /// </summary>
    public IReadOnlyList<string> Supersedes { get; init; } = [];

    /// <summary>
    /// The agent as the steps consume it, or null while the program is missing — the one
    /// question validation asks and registration answers.
    /// </summary>
    /// <param name="label">The label built from the key this was declared under.</param>
    /// <param name="stamp">When the executable was last written.</param>
    public AgentDefinition? ToDefinition(AgentLabel label, DateTimeOffset stamp) =>
        Program is { } program
            ? new AgentDefinition(label, program, [.. Arguments], Environment, WorkingDirectory, Log, KeepAlive, ExitTimeout, Restart, stamp)
            {
                Supersedes = [.. Supersedes]
            }
            : null;
}
