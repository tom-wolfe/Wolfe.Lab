namespace Wolfe.Lab.Build.Clients.Restic;

/// <summary>
/// A restic repository.
/// </summary>
/// <param name="Environment">Environment variables, keyed by name.</param>
public sealed record ResticRepository(IReadOnlyDictionary<string, string> Environment)
{
    internal const string LocationVariable = "RESTIC_REPOSITORY";

    /// <summary>
    /// Where the repository is: a path, or a backend URL.
    /// </summary>
    public string Location => Environment[LocationVariable];

    /// <summary>
    /// Whether the location is a directory on this node rather than a backend reached over a
    /// scheme, which is when its existence can be checked before restic is asked.
    /// </summary>
    public bool IsLocal => Path.IsPathRooted(Location);
}
