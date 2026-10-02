namespace Wolfe.Lab.Infrastructure.Packages;

/// <summary>
/// A package on this node.
/// </summary>
/// <param name="Package">What was asked for.</param>
/// <param name="Directory">Where it is — or, rehearsed, where it would be.</param>
/// <param name="Outcome">Whether this run installed it.</param>
public sealed record InstalledPackage(Package Package, IDirectory Directory, PackageOutcome Outcome);
