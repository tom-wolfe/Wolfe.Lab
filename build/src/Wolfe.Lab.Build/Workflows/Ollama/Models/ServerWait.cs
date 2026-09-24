namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// How long a deploy gives a freshly converged server to start answering, and how often it asks.
/// </summary>
/// <param name="Timeout">The longest it waits.</param>
/// <param name="Interval">The pause between asks.</param>
public sealed record ServerWait(TimeSpan Timeout, TimeSpan Interval)
{
    /// <summary>
    /// Long enough for a cold start with the model store on an external drive.
    /// </summary>
    public static ServerWait Default { get; } = new(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1));
}
