using Ritten.Docker;

namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// What the lab asks of a compose service beyond what compose says of it.
/// </summary>
public static class ComposeExtensions
{
    extension(ComposeService service)
    {
        /// <summary>
        /// What its container is called: its own name, or the service's when it gives none.
        /// </summary>
        public string Container => service.ContainerName ?? service.Name;

        /// <summary>
        /// The host's loopback port the container's <paramref name="target"/> is published on, if one is.
        /// </summary>
        public int? OnLoopback(int target) =>
            service.Ports.FirstOrDefault(port => port.Target == target && port.OnLoopback)?.Published;
    }

    extension(ComposePort port)
    {
        /// <summary>
        /// Whether something on the host reaches the port at its loopback address.
        /// </summary>
        public bool OnLoopback => port.Published is not null && port is { Protocol: "tcp", HostIp: null or "" or "0.0.0.0" or "::" or "127.0.0.1" };
    }
}
