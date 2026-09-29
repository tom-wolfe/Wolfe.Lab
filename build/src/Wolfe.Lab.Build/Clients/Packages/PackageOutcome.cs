namespace Wolfe.Lab.Build.Clients.Packages;

/// <summary>
/// What installing a package came to.
/// </summary>
public enum PackageOutcome
{
    /// <summary>Already on the node.</summary>
    Present,

    /// <summary>Downloaded, verified and unpacked by this run.</summary>
    Installed,

    /// <summary>Not on the node; a rehearsal that would install it.</summary>
    WouldInstall
}
