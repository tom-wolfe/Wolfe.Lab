namespace Wolfe.Lab.Build.Clients.Packages;

/// <summary>
/// Puts a package on the node, once per version.
/// </summary>
public interface IPackageInstaller
{
    /// <summary>
    /// The package's directory — <c>${LAB_ROOT}/packages/&lt;name&gt;/&lt;version&gt;</c> — installing
    /// it first when this version is not there.
    /// </summary>
    Task<InstalledPackage> Install(Package package, CancellationToken ct = default);
}
