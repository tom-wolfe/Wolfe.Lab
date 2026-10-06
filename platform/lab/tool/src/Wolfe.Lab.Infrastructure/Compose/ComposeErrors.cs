namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// The well-known problems with reading a compose project.
/// </summary>
public static class ComposeErrors
{
    /// <summary>
    /// Compose refused the stack in <paramref name="directory"/>: what it said, and which stack it was.
    /// </summary>
    public static IEnumerable<Error> Unreadable(IDirectory directory, IEnumerable<Error> reasons) =>
        reasons.Select(reason => new Error($"compose cannot read the stack in {directory.AbsolutePath}: {reason.Message}"));
}
