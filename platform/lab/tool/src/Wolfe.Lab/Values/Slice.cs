using Ritten.Engine.FileSystem;

namespace Wolfe.Lab.Values;

/// <summary>
/// A slice whose files other components read: the one place a component looks outside its own
/// directory, on purpose, because the slice owns what those files declare.
/// </summary>
/// <remarks>
/// Found by walking up from the component, looking at each level for the slice itself or inside
/// one of that level's areas (<c>platform/restic</c>), so which area holds it is the layout's
/// business. The walk stops at the checkout's root: past it, looking inside every sibling would
/// be looking through the node's home directory.
/// </remarks>
public sealed record Slice(string Name)
{
    /// <summary>
    /// The repositories every backup snapshots into.
    /// </summary>
    public static Slice Restic { get; } = new("restic");

    /// <summary>
    /// The object store, and the tofu state backend every root shares.
    /// </summary>
    public static Slice Garage { get; } = new("garage");

    /// <summary>
    /// The nearest copy of <paramref name="fileName"/> in this slice above <paramref name="from"/>,
    /// or null when the checkout has none.
    /// </summary>
    public IFile? FindFile(IDirectory from, string fileName)
    {
        for (var directory = from; directory is not null; directory = Parent(directory))
        {
            if (Candidates(directory).Select(slice => slice.GetFile(fileName)).FirstOrDefault(file => file.Exists) is { } found)
            {
                return found;
            }

            if (IsCheckoutRoot(directory))
            {
                break;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private IEnumerable<IDirectory> Candidates(IDirectory directory)
    {
        yield return directory.GetDirectory(Name);
        if (!directory.Exists)
        {
            yield break;
        }

        foreach (var area in Directory.EnumerateDirectories(directory.AbsolutePath).Order(StringComparer.Ordinal))
        {
            yield return new PhysicalDirectory(area).GetDirectory(Name);
        }
    }

    /// <summary>
    /// A checkout's root holds <c>.git</c>: a directory in a clone, a file in a worktree.
    /// </summary>
    private static bool IsCheckoutRoot(IDirectory directory) =>
        directory.GetDirectory(".git").Exists || directory.GetFile(".git").Exists;

    private static IDirectory? Parent(IDirectory directory) =>
        Path.GetDirectoryName(directory.AbsolutePath) is { Length: > 0 } parent && parent != directory.AbsolutePath
            ? new PhysicalDirectory(parent)
            : null;
}
