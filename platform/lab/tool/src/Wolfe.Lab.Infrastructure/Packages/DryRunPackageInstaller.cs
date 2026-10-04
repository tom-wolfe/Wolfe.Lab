using Microsoft.Extensions.Options;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Infrastructure.Packages;

/// <summary>
/// The rehearsal: says what it would install, having checked the release has both files, so a
/// version or asset that was mistyped fails here rather than on the deploy.
/// </summary>
internal sealed class DryRunPackageInstaller(IHttpClientFactory clients, IOptions<GithubOptions> options, IOptions<LabDirectories> roots, IWorkflowLog log) : IPackageInstaller
{
    private readonly HttpClient _http = clients.CreateClient(GithubPackageInstaller.Client);
    private readonly GithubOptions _sources = options.Value;

    /// <inheritdoc />
    public async Task<InstalledPackage> Install(Package package, CancellationToken ct = default)
    {
        var directory = GithubPackageInstaller.Location(package, roots.Value);
        if (directory.GetFile(GithubPackageInstaller.Complete).Exists)
        {
            return new InstalledPackage(package, directory, PackageOutcome.Present);
        }

        if (package.Checksums is null)
        {
            // Resolving the digest is the check that the release, and its asset, are there.
            await GithubPackageInstaller.Digest(_http, _sources.RequiredApi, package, ct);
        }

        foreach (var asset in new[] { package.Asset, package.Checksums }.OfType<string>())
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, package.Download(_sources.RequiredReleases, asset));
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"{package.Repository} {package.Tag} has no {asset} ({(int)response.StatusCode}): {package.Download(_sources.RequiredReleases, asset)}");
            }
        }

        log.Skipped($"Would install {package.Repository} {package.Tag} ({package.Asset}) into {directory.AbsolutePath}.");
        return new InstalledPackage(package, directory, PackageOutcome.WouldInstall);
    }
}
