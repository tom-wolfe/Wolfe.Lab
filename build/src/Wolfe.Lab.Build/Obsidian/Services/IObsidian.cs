namespace Wolfe.Lab.Build.Obsidian.Services;

/// <summary>
/// The headless Obsidian client.
/// </summary>
public interface IObsidian
{
    /// <summary>
    /// Runs one sync pass for the vault at the given checkout.
    /// </summary>
    /// <param name="vault">The checkout Obsidian Sync is set up in.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task Sync(IDirectory vault, CancellationToken cancellationToken = default);
}
