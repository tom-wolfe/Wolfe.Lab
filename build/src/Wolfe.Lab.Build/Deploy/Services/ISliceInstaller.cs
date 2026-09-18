namespace Wolfe.Lab.Build.Deploy.Services;

/// <summary>
/// Installs a slice from the checkout into its release directory.
/// </summary>
public interface ISliceInstaller
{
    /// <summary>
    /// Makes the release an exact copy of the source, minus what the node never runs.
    /// </summary>
    Task Install(IDirectory source, IDirectory release, CancellationToken ct = default);
}
