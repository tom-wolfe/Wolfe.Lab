namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// One of a service's ports, as compose resolves the file's <c>ports</c>.
/// </summary>
/// <param name="Target">The container's port.</param>
/// <param name="Published">The host's port it is published on, or null when it is published on one Docker picks, or a range.</param>
/// <param name="HostIp">The host address it is published on, or null for every one.</param>
/// <param name="Protocol">tcp or udp.</param>
public sealed record ComposePort(int Target, int? Published, string? HostIp, string Protocol)
{
    /// <summary>
    /// Whether something on the host reaches the port at its loopback address.
    /// </summary>
    public bool OnLoopback => Published is not null && Protocol == "tcp" && HostIp is null or "" or "0.0.0.0" or "::" or "127.0.0.1";
}
