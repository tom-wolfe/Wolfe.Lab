namespace Wolfe.Lab.Build.Workflows.Backup;

/// <summary>
/// Renames on the node's file system.
/// </summary>
internal sealed class StateDirectories : IStateDirectories
{
    /// <inheritdoc />
    public bool Move(IDirectory from, IDirectory to)
    {
        if (!from.Exists)
        {
            return false;
        }

        Directory.Move(from.AbsolutePath, to.AbsolutePath);
        return true;
    }
}
