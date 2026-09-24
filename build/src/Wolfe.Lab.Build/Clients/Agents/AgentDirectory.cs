using Ritten.Engine.FileSystem;

namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// Where the platform's supervisor reads units from.
/// </summary>
/// <param name="Directory">The directory itself.</param>
public sealed record AgentDirectory(IDirectory Directory)
{
    /// <summary>
    /// launchd's per-user agent directory. Per-user, not system: see <see cref="UserDomain"/>.
    /// </summary>
    public static AgentDirectory Launchd => new(new PhysicalDirectory(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents")));

    /// <summary>
    /// The systemd user manager's unit directory.
    /// </summary>
    public static AgentDirectory Systemd => new(new PhysicalDirectory(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "systemd", "user")));
}
