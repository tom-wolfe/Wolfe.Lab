namespace Wolfe.Lab.Build.Clients.Agents.Systemd;

/// <summary>
/// An agent as the systemd unit template reads it.
/// </summary>
/// <param name="Label">What the supervisor knows it as.</param>
/// <param name="Program">The executable, for the provenance comment.</param>
/// <param name="Stamped">When that executable was last written.</param>
/// <param name="ExecStart">The whole command line, each word quoted.</param>
/// <param name="WorkingDirectory">The directory it runs from, or null.</param>
/// <param name="Environment">Each variable as one quoted <c>KEY=value</c> word, ordered so the render is stable.</param>
/// <param name="KeepAlive">Whether systemd starts it again when it exits.</param>
/// <param name="ExitTimeout">How long it gets to stop, or null.</param>
/// <param name="Log">Where both streams land, or null.</param>
public sealed record SystemdUnit(
    string Label,
    string Program,
    string Stamped,
    string ExecStart,
    string? WorkingDirectory,
    IReadOnlyList<UnitVariable> Environment,
    bool KeepAlive,
    int? ExitTimeout,
    string? Log
);
