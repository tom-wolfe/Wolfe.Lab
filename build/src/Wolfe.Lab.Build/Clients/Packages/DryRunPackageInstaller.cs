using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Releases;

namespace Wolfe.Lab.Build.Clients.Packages;

/// <summary>
/// The rehearsal: says what it would install, having checked the release has both files, so a
/// version or asset that was mistyped fails here rather than on the deploy.
/// </summary>
internal sealed class DryRunPackageInstaller(HttpClient http, WorkflowEnvironment environment, IWorkflowLog log) : IPackageInstaller
{
    /// <inheritdoc />
    public async Task<InstalledPackage> Install(Package package, CancellationToken ct = default)
    {
        var directory = GithubPackageInstaller.Location(package, LabRoots.From(environment));
        if (File.Exists(Path.Combine(directory, GithubPackageInstaller.Complete)))
        {
            return new InstalledPackage(package, new PhysicalDirectory(directory), PackageOutcome.Present);
        }

        foreach (var asset in new[] { package.Asset, package.Checksums })
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, package.Download(asset));
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"{package.Repository} {package.Tag} has no {asset} ({(int)response.StatusCode}): {package.Download(asset)}");
            }
        }

        log.Skipped($"Would install {package.Repository} {package.Tag} ({package.Asset}) into {directory}.");
        return new InstalledPackage(package, new PhysicalDirectory(directory), PackageOutcome.WouldInstall);
    }
}
