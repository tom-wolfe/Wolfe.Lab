using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Agents;

/// <summary>
/// An agent as the steps consume it: everything the unit needs, with nothing left to ask.
/// </summary>
/// <param name="Label">What the supervisor knows it as.</param>
/// <param name="Program">The executable it runs.</param>
/// <param name="Arguments">The arguments it runs with.</param>
/// <param name="Environment">Environment variables set for the process.</param>
/// <param name="WorkingDirectory">The directory it runs from, when it cares.</param>
/// <param name="Log">Where both its streams land, when it keeps a log.</param>
/// <param name="KeepAlive">Whether the supervisor starts it again when it exits.</param>
/// <param name="ExitTimeout">How long it gets to stop before it is killed.</param>
/// <param name="Restart">How a changed unit reaches the running process.</param>
/// <param name="ProgramStamp">
/// When the executable was last written. Folded into the unit so that upgrading the binary
/// counts as a change to the agent — the reason a Homebrew upgrade restarts it without anyone
/// having edited the declaration.
/// </param>
public sealed record AgentDefinition(
    AgentLabel Label,
    HostPath Program,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> Environment,
    HostPath? WorkingDirectory,
    HostPath? Log,
    bool KeepAlive,
    int? ExitTimeout,
    AgentRestart Restart,
    DateTimeOffset ProgramStamp
);
