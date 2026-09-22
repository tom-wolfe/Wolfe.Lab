namespace Wolfe.Lab.Build.Backup;

/// <summary>
/// The one door to a service's state on disk, for the restore that sets it aside and the undo
/// that puts it back.
/// </summary>
public interface IStateDirectories
{
    /// <summary>
    /// Renames the directory, when it exists.
    /// </summary>
    /// <param name="from">The directory.</param>
    /// <param name="to">Its new path.</param>
    /// <returns>Whether there was anything to move.</returns>
    bool Move(IDirectory from, IDirectory to);
}
