namespace Wolfe.Lab.Application.Releases;

/// <summary>
/// A deployment as installed on the node, and when its files last changed there.
/// </summary>
/// <param name="Directory">Where it is installed.</param>
/// <param name="Stamp">
/// The newest write under it, or null when nothing is there. The installer only writes a file
/// whose content changed, so this moves exactly when a running process would need to read its
/// files again — and it is read from the node, so a restart that failed is still owed the next
/// time.
/// </param>
public sealed record Installation(IDirectory Directory, DateTimeOffset? Stamp);
