using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Releases;

namespace Wolfe.Lab.Build.Clients.Packages;

/// <summary>
/// Downloads a release asset, checks it against the release's own checksums, and unpacks it into a
/// directory of its version.
/// </summary>
/// <remarks>
/// Unpacked beside its destination and renamed into place, with a marker written last, so a
/// directory that exists is a version that was installed whole — a run that died halfway leaves
/// nothing the next one would trust. Versions sit side by side: an upgrade adds a directory, and
/// rolling back is naming the old version again.
/// </remarks>
internal sealed class GithubPackageInstaller(IHttpClientFactory clients, IOptions<GithubOptions> options, ICommandRunner commands, WorkflowEnvironment environment, IWorkflowLog log) : IPackageInstaller
{
    /// <summary>
    /// The HTTP client every package request goes through, with its resilience.
    /// </summary>
    internal const string Client = "packages";

    private readonly HttpClient _http = clients.CreateClient(Client);
    private readonly GithubOptions _sources = options.Value;

    internal const string Complete = ".complete";

    /// <inheritdoc />
    public async Task<InstalledPackage> Install(Package package, CancellationToken ct = default)
    {
        var directory = Location(package, LabRoots.From(environment));
        if (File.Exists(Path.Combine(directory, Complete)))
        {
            return new InstalledPackage(package, new PhysicalDirectory(directory), PackageOutcome.Present);
        }

        var parent = Versions(package, LabRoots.From(environment));
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, $".{package.Version}.{Guid.NewGuid():N}");
        var download = staging + ".download";
        try
        {
            log.Detail($"Downloading {package.Download(_sources.RequiredReleases, package.Asset)}.");
            using (var response = await Get(package, package.Asset, ct))
            await using (var file = File.Create(download))
            await using (var body = await response.Content.ReadAsStreamAsync(ct))
            {
                await body.CopyToAsync(file, ct);
            }

            var expected = await Expected(package, ct);
            var actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(File.OpenRead(download), ct));
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{package.Asset} does not match {package.Verification}: expected {expected}, got {actual}.");
            }

            Directory.CreateDirectory(staging);
            await Unpack(package, download, staging, ct);
            await File.WriteAllTextAsync(Path.Combine(staging, Complete), $"{package.Repository} {package.Tag} {package.Asset} sha256:{actual}\n", ct);

            // A directory without the marker is a previous attempt that never finished.
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }

            Directory.Move(staging, directory);
            return new InstalledPackage(package, new PhysicalDirectory(directory), PackageOutcome.Installed);
        }
        finally
        {
            File.Delete(download);
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }
        }
    }

    /// <summary>
    /// The SHA-256 the release records for the asset: from its checksum file, or — for a project
    /// that publishes none — the digest GitHub keeps for every asset.
    /// </summary>
    private async Task<string> Expected(Package package, CancellationToken ct)
    {
        if (package.Checksums is { } checksums)
        {
            using var listing = await Get(package, checksums, ct);
            return Expected(await listing.Content.ReadAsStringAsync(ct), package.Asset)
                   ?? throw new InvalidOperationException($"{checksums} in {package.Repository} {package.Tag} lists no {package.Asset}.");
        }

        return await Digest(_http, _sources.RequiredApi, package, ct);
    }

    /// <summary>
    /// The asset's digest from GitHub's releases API, without the <c>sha256:</c> prefix.
    /// </summary>
    internal static async Task<string> Digest(HttpClient http, Uri api, Package package, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, package.Release(api));
        request.Headers.UserAgent.ParseAdd("Wolfe.Lab.Build");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{package.Repository} has no release {package.Tag} ({(int)response.StatusCode}): {package.Release(api)}");
        }

        using var release = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var digest = release.RootElement.GetProperty("assets").EnumerateArray()
            .Where(asset => asset.GetProperty("name").GetString() == package.Asset)
            .Select(asset => asset.TryGetProperty("digest", out var value) ? value.GetString() : null)
            .FirstOrDefault();
        return digest is { } value && value.StartsWith("sha256:", StringComparison.Ordinal)
            ? value["sha256:".Length..]
            : throw new InvalidOperationException($"{package.Repository} {package.Tag} records no SHA-256 for {package.Asset}, and the package names no checksum file.");
    }

    /// <summary>
    /// A file of the release, or a failure naming which release lacks which file.
    /// </summary>
    private async Task<HttpResponseMessage> Get(Package package, string asset, CancellationToken ct)
    {
        var response = await _http.GetAsync(package.Download(_sources.RequiredReleases, asset), HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            throw new InvalidOperationException(
                $"{package.Repository} {package.Tag} has no {asset} ({(int)response.StatusCode}): {package.Download(_sources.RequiredReleases, asset)}");
        }

        return response;
    }

    /// <summary>
    /// Where a version of a package lives: <c>${LAB_ROOT}/packages/&lt;name&gt;/&lt;version&gt;</c>.
    /// </summary>
    internal static string Location(Package package, LabRoots roots) =>
        Path.Combine(Versions(package, roots), package.Version);

    /// <summary>
    /// Where every version of a package sits: <c>${LAB_ROOT}/packages/&lt;name&gt;</c>.
    /// </summary>
    private static string Versions(Package package, LabRoots roots) =>
        Path.Combine(roots.Root, "packages", package.Name);

    /// <summary>
    /// The checksum a <c>sha256sum</c>-style file lists for the asset: <c>&lt;hex&gt;  &lt;name&gt;</c>,
    /// the name marked <c>*</c> in binary mode.
    /// </summary>
    internal static string? Expected(string checksums, string asset) =>
        checksums.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length == 2 && parts[1].TrimStart('*').Replace("./", "", StringComparison.Ordinal) == asset)
            .Select(parts => parts[0])
            .FirstOrDefault();

    /// <summary>
    /// An archive unpacked as it was packed; a single file — compressed or bare — saved under the
    /// package's name and made executable, which is what a tool on <c>PATH</c> needs to be called.
    /// </summary>
    private async Task Unpack(Package package, string download, string staging, CancellationToken ct)
    {
        if (package.Asset.EndsWith(".zip", StringComparison.Ordinal))
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(download, staging);
        }
        else if (package.Asset.EndsWith(".tar.gz", StringComparison.Ordinal) || package.Asset.EndsWith(".tgz", StringComparison.Ordinal))
        {
            await commands.Run(Command.Create("tar").WithArguments("-xzf", download, "-C", staging).ThrowOnError(), ct);
        }
        else if (package.Asset.EndsWith(".bz2", StringComparison.Ordinal))
        {
            var compressed = Path.Combine(staging, package.Name + ".bz2");
            File.Copy(download, compressed);
            await commands.Run(Command.Create("bzip2").WithArguments("-d", compressed).ThrowOnError(), ct);
            Executable(Path.Combine(staging, package.Name));
        }
        else
        {
            File.Copy(download, Path.Combine(staging, package.Name));
            Executable(Path.Combine(staging, package.Name));
        }
    }

    /// <summary>
    /// Makes a file executable. Some releases ship their program without the bit — Alloy's zip
    /// stores it <c>rw-r--r--</c> — so what the lab runs out of a package is made runnable by
    /// whatever names it: an agent's <c>program</c>, a tool's command.
    /// </summary>
    internal static void Executable(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, File.GetUnixFileMode(path) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
        }
    }
}
