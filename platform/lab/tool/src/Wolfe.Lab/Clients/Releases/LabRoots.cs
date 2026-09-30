namespace Wolfe.Lab.Clients.Releases;

/// <summary>
/// The two directories every node keeps the lab in: where it installs, and where state lives.
/// </summary>
/// <remarks>
/// Both are environment variables chezmoi sets on every node — for its runners, its shells and
/// the lab's own processes — so a path in a <c>ritten.json</c> names them rather than a
/// directory that differs between a Mac and the Pi: <c>${LAB_ROOT}/alloy</c>. Unset, each falls
/// back to what it has always been.
/// </remarks>
/// <param name="Root">Where components and their artifacts are installed: <c>LAB_ROOT</c>.</param>
/// <param name="Data">Where services keep their state: <c>LAB_DATA</c>.</param>
public sealed record LabRoots(string Root, string Data)
{
    internal const string RootVariable = "LAB_ROOT";
    internal const string DataVariable = "LAB_DATA";

    /// <summary>
    /// The roots as this node's environment names them.
    /// </summary>
    public static LabRoots From(WorkflowEnvironment environment)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return new LabRoots(
            environment.Get(RootVariable) is { Length: > 0 } root ? root : Path.Combine(home, ".local", "share", "Wolfe.Lab"),
            environment.Get(DataVariable) is { Length: > 0 } data ? data : Path.Combine(home, "Docker"));
    }

    /// <summary>
    /// The value with <c>${LAB_ROOT}</c> and <c>${LAB_DATA}</c> replaced. Nothing else is
    /// expanded: a value is otherwise passed on exactly as written.
    /// </summary>
    public string Expand(string value) => value
        .Replace($"${{{RootVariable}}}", Root, StringComparison.Ordinal)
        .Replace($"${{{DataVariable}}}", Data, StringComparison.Ordinal);

    /// <summary>
    /// Whether the path lies strictly inside the install root — the one place an artifact may
    /// be mirrored into, since a mirror deletes whatever its source does not have.
    /// </summary>
    public bool Contains(string path)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Root)) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, StringComparison.Ordinal);
    }

    /// <summary>
    /// Where a compose release records the artifact stamp its stack was last restarted for:
    /// beside the releases, never inside one, so writing it changes no release.
    /// </summary>
    public string AppliedStamp(string release) => Path.Combine(Applied, release);

    /// <summary>
    /// Where every compose release's restart stamp is kept.
    /// </summary>
    public string Applied => Path.Combine(Root, ".applied");

    /// <summary>
    /// Where a component's agents say which log files they write, for the node's collector to
    /// find (monitoring/alloy): one target file per component, beside the releases.
    /// </summary>
    public string Logs => Path.Combine(Root, ".logs");
}
