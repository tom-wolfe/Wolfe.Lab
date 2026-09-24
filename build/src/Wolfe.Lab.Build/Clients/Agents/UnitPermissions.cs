namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// The permissions every unit is written with.
/// </summary>
internal static class UnitPermissions
{
    /// <summary>
    /// The owner's alone.
    /// </summary>
    public const UnixFileMode Owner = UnixFileMode.UserRead | UnixFileMode.UserWrite;
}
