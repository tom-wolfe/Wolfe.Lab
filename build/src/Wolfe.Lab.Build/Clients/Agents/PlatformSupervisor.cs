namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// The supervisor this node's operating system has, chosen once at registration. The rehearsal
/// wraps it, whichever it is.
/// </summary>
/// <param name="Supervisor">launchd's on macOS, systemd's on Linux.</param>
internal sealed record PlatformSupervisor(IServiceSupervisor Supervisor);
