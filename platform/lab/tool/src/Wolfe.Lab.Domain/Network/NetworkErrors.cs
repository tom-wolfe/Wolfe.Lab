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
}
