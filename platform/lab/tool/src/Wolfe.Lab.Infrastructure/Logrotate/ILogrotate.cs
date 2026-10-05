namespace Wolfe.Lab.Infrastructure.Logrotate;

/// <summary>
/// Rotates log files with logrotate, as the user the lab runs as: never root's, never on a
/// schedule of its own.
/// </summary>
public interface ILogrotate
{
    /// <summary>
    /// Rotates what <paramref name="configuration"/> names, remembering when in <paramref name="state"/>.
    /// </summary>
    /// <param name="configuration">The logrotate configuration to run.</param>
    /// <param name="state">The state file, the lab's own rather than the system's.</param>
    /// <param name="ct">A token to monitor for cancellation.</param>
    Task Rotate(IFile configuration, IFile state, CancellationToken ct = default);
}
