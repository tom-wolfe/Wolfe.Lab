namespace Wolfe.Lab.Build.Clients.Packages;

/// <summary>
/// A package as the installer consumes it: everything resolved for this node.
/// </summary>
/// <param name="Name">What it is installed under.</param>
/// <param name="Repository">The repository publishing it.</param>
/// <param name="Version">The release.</param>
/// <param name="Tag">The release's tag.</param>
/// <param name="Asset">The asset for this node.</param>
/// <param name="Checksums">The release's checksum file.</param>
public sealed record Package(string Name, string Repository, string Version, string Tag, string Asset, string? Checksums)
{
    /// <summary>
    /// The package as declared, for this node's platform — or the reason it cannot be.
    /// </summary>
    public static Result<Package> From(string name, PackageSettings settings, string platform)
    {
        if (settings.Github is not { Length: > 0 } repository || settings.Version is not { Length: > 0 } version)
        {
            return new Error($"Package '{name}' needs 'github' and 'version'.");
        }

        var asset = settings.Asset ?? settings.Assets.GetValueOrDefault(platform);
        return asset is { Length: > 0 }
            ? new Package(name, repository, version, settings.Expand(settings.Tag), settings.Expand(asset),
                settings.Checksums is { Length: > 0 } checksums ? settings.Expand(checksums) : null)
            {
                Bin = settings.Bin is { Length: > 0 } bin ? settings.Expand(bin) : null
            }
            : new Error($"Package '{name}' names no asset for {platform}.");
    }

    /// <summary>
    /// The directory inside the package holding a tool's command, or null for its top level.
    /// </summary>
    public string? Bin { get; init; }

    /// <summary>
    /// What the asset is checked against, for the log and the report.
    /// </summary>
    public string Verification => Checksums != null ? $"`{Checksums}`" : "GitHub's digest";

    /// <summary>
    /// Where the release is described in GitHub's API, with each asset's digest.
    /// </summary>
    public Uri Release => new($"https://api.github.com/repos/{Repository}/releases/tags/{Tag}");

    /// <summary>
    /// Where a release asset of this package downloads from.
    /// </summary>
    public Uri Download(string asset) => new($"https://github.com/{Repository}/releases/download/{Tag}/{asset}");

    /// <summary>
    /// This node's platform, as a package's <c>assets</c> are keyed: <c>darwin-arm64</c>.
    /// </summary>
    public static string Platform =>
        $"{(OperatingSystem.IsMacOS() ? "darwin" : "linux")}-{(System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "arm64" : "amd64")}";
}
