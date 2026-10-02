namespace Wolfe.Lab.Infrastructure.Packages;

/// <summary>
/// A program the lab installs from a GitHub release: an agent's <c>package</c>, or an entry of
/// <c>.config/lab-tools.json</c>.
/// </summary>
/// <remarks>
/// Only the version is pinned. The checksum is the publisher's own, read from the release beside
/// the asset — a second pinned value is one Renovate cannot move, and the one that goes stale.
/// <c>{version}</c> in any name is the version.
/// </remarks>
public sealed record PackageOptions
{
    /// <summary>
    /// The repository that publishes it, <c>owner/name</c>. Renovate reads this and
    /// <see cref="Version"/> as a pair, so they are written in that order.
    /// </summary>
    public string? Github { get; init; }

    /// <summary>
    /// The release, without its tag's prefix.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// The release's tag. <c>v{version}</c> unless the project tags otherwise.
    /// </summary>
    public string Tag { get; init; } = "v{version}";

    /// <summary>
    /// The release asset listing each asset's SHA-256.
    /// </summary>
    public string? Checksums { get; init; }

    /// <summary>
    /// The directory inside the package that holds a tool's command,
    /// for an archive that nests it. The package's top level when unset.
    /// </summary>
    public string? Bin { get; init; }

    /// <summary>
    /// The asset to install, for a package that runs on one platform — an agent, declared per node.
    /// </summary>
    public string? Asset { get; init; }

    /// <summary>
    /// The asset for each platform (<c>darwin-arm64</c>, <c>linux-arm64</c>), for a package any
    /// node may need — a tool.
    /// </summary>
    public IReadOnlyDictionary<string, string> Assets { get; init; } = new Dictionary<string, string>();

    internal string Expand(string template) => template.Replace("{version}", Version, StringComparison.Ordinal);
}
