namespace Wolfe.Lab.Build.Clients.Agents.Launchd;

/// <summary>
/// An agent as its unit template reads it.
/// </summary>
/// <param name="Label">What the supervisor knows it as.</param>
/// <param name="Program">The executable, for the provenance comment.</param>
/// <param name="Stamped">When that executable was last written.</param>
/// <param name="Arguments">The whole argument vector, the program at its head.</param>
/// <param name="WorkingDirectory">The directory it runs from, or null.</param>
/// <param name="Environment">Its environment, ordered, so the render is stable.</param>
/// <param name="KeepAlive">Whether the supervisor starts it again when it exits.</param>
/// <param name="ExitTimeout">How long it gets to stop, or null.</param>
/// <param name="Log">Where both streams land, or null.</param>
public sealed record LaunchdUnit(
    string Label,
    string Program,
    string Stamped,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory,
    IReadOnlyList<UnitVariable> Environment,
    bool KeepAlive,
    int? ExitTimeout,
    string? Log)
{
    /// <summary>
    /// Whether to write the environment block at all, asked as a flag because a template
    /// should read as the file it becomes rather than count things.
    /// </summary>
    public bool HasEnvironment => Environment.Count > 0;
}
