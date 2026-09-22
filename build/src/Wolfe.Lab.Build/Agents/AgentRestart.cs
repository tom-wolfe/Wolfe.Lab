namespace Wolfe.Lab.Build.Agents;

/// <summary>
/// How a running agent is made to pick up a unit that changed.
/// </summary>
public enum AgentRestart
{
    /// <summary>
    /// Unload and load again. The default, and the only one that makes a changed unit take
    /// effect now, because a supervisor reads the unit when it loads it.
    /// </summary>
    Reload,

    /// <summary>
    /// Signal the process and let <c>KeepAlive</c> start it again, so work in flight finishes
    /// first. For the one agent that is hosting the job doing the converging: a reload would
    /// take that job with it. The cost is that the unit just written is read on the NEXT start.
    /// </summary>
    Signal
}
