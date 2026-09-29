namespace Wolfe.Lab.Build.Clients.Releases;

/// <summary>
/// Installs a component from the checkout into its release directory.
/// </summary>
public interface IReleaseInstaller
{
    /// <summary>
    /// Makes the release an exact copy of the source, minus what the node never runs.
    /// </summary>
    /// <returns>
    /// What changed — or, rehearsed, what would — one path each, marked <c>+</c> added,
    /// <c>~</c> changed or <c>-</c> deleted. Empty when the release already matched.
    /// </returns>
    Task<IReadOnlyList<string>> Install(IDirectory source, IDirectory release, CancellationToken ct = default);
}
