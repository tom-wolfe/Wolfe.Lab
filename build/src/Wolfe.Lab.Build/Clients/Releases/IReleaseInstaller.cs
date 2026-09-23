namespace Wolfe.Lab.Build.Clients.Releases;

/// <summary>
/// Installs a component from the checkout into its release directory.
/// </summary>
public interface IReleaseInstaller
{
    /// <summary>
    /// Makes the release an exact copy of the source, minus what the node never runs.
    /// </summary>
    Task Install(IDirectory source, IDirectory release, CancellationToken ct = default);
}
