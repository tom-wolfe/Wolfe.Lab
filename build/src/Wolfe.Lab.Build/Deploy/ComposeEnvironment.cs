namespace Wolfe.Lab.Build.Deploy;

/// <summary>
/// What the compose file reads from its environment.
/// </summary>
/// <param name="Variables">The variables and their values.</param>
public sealed record ComposeEnvironment(IReadOnlyDictionary<string, string> Variables)
{
    /// <summary>
    /// The environment of a slice that names no secrets.
    /// </summary>
    public static ComposeEnvironment Empty { get; } = new(new Dictionary<string, string>());
}
