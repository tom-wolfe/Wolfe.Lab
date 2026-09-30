namespace Wolfe.Lab.Clients.Packages;

/// <summary>
/// Where releases come from, from <c>Packages</c> in <c>appsettings.json</c>.
/// </summary>
public sealed class GithubOptions
{
    /// <summary>
    /// The base release assets download from: <c>&lt;base&gt;&lt;owner/name&gt;/releases/download/…</c>.
    /// </summary>
    public Uri? Releases { get; set; }

    /// <summary>
    /// The base of the API that records each asset's digest: <c>&lt;base&gt;repos/&lt;owner/name&gt;/…</c>.
    /// </summary>
    public Uri? Api { get; set; }

    /// <summary>
    /// The downloads' base, which registration validated is set.
    /// </summary>
    internal Uri RequiredReleases => Releases ?? throw new InvalidOperationException("'Packages:Releases' is not set in appsettings.json.");

    /// <summary>
    /// The API's base, which registration validated is set.
    /// </summary>
    internal Uri RequiredApi => Api ?? throw new InvalidOperationException("'Packages:Api' is not set in appsettings.json.");
}
