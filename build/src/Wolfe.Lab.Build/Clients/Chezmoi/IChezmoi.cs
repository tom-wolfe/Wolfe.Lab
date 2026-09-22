namespace Wolfe.Lab.Build.Clients.Chezmoi;

/// <summary>
/// The chezmoi on the path: renders the source for any machine, and applies it to this one.
/// </summary>
public interface IChezmoi
{
    /// <summary>
    /// Renders the whole source for one profile into a directory, without applying anything:
    /// the files a machine of that profile would get, with their exact contents.
    /// </summary>
    /// <param name="source">The source directory — the checkout, whose <c>.chezmoiroot</c> names the home tree.</param>
    /// <param name="profile">The profile to render as.</param>
    /// <param name="destination">Where the rendered tree goes.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>The rendered files, relative to the destination, sorted.</returns>
    Task<IReadOnlyList<string>> Render(IDirectory source, string profile, IDirectory destination, CancellationToken ct = default);

    /// <summary>
    /// Pulls this node's source and applies it: <c>chezmoi update</c>.
    /// </summary>
    Task Update(CancellationToken ct = default);
}
