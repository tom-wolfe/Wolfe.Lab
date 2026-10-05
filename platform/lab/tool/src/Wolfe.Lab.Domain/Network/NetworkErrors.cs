namespace Wolfe.Lab.Domain.Network;

/// <summary>
/// The well-known problems with what addresses a service on the network.
/// </summary>
public static class NetworkErrors
{
    /// <summary>
    /// Not a TCP port (<see cref="Port"/>).
    /// </summary>
    public static Error NotAPort(int given) => new($"{given} is not a port: 1 to 65535.");

    /// <summary>
    /// Not the path of an HTTP request (<see cref="HttpPath"/>).
    /// </summary>
    public static Error NotAPath(string given) => new($"'{given}' is not a path: it starts with '/', and has no spaces, query or fragment.");

    /// <summary>
    /// The value is not a DNS name or an IP address.
    /// </summary>
    public static Error NotAHostName(string given) => new($"'{given}' is not a host name: a DNS name or an IP address.");

    /// <summary>
    /// The value is not an endpoint Docker can be reached at.
    /// </summary>
    public static Error NotADockerHost(string given, IEnumerable<string> schemes) =>
        new($"'{given}' is not a Docker endpoint: an absolute URI, {string.Join(", ", schemes.Select(scheme => $"{scheme}://"))}.");
}
