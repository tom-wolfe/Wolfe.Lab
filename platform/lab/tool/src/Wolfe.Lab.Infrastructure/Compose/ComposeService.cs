namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// One service of a <see cref="ComposeProject"/>.
/// </summary>
/// <param name="Name">The service's name in the compose file.</param>
/// <param name="Labels">Its Docker labels.</param>
/// <param name="Environment">Its environment; a variable declared without a value is null.</param>
/// <param name="Ports">The ports it publishes.</param>
/// <param name="NetworkMode">Whose network it runs in, when not its own project's: <c>service:immich-server</c>, <c>host</c>.</param>
public sealed record ComposeService(
    string Name,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyDictionary<string, string?> Environment,
    IReadOnlyList<ComposePort> Ports,
    string? NetworkMode = null
)
{
    /// <summary>
    /// The host's loopback port the container's <paramref name="target"/> is published on, if one is.
    /// </summary>
    public int? OnLoopback(int target) =>
        Ports.FirstOrDefault(port => port.Target == target && port.OnLoopback)?.Published;
}
