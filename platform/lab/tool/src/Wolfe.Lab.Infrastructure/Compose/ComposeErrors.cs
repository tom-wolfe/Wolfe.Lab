namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// The well-known problems with reading a compose project.
/// </summary>
public static class ComposeErrors
{
    /// <summary>
    /// Compose refused the stack.
    /// </summary>
    public static Error Unreadable(IDirectory directory, string reason) => new($"compose cannot read the stack in {directory.AbsolutePath}: {reason}");

    /// <summary>
    /// Compose printed something that is not a project's configuration.
    /// </summary>
    public static Error NotConfiguration(string reason) => new($"compose printed something that is not its configuration: {reason}");
}
