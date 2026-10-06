namespace Wolfe.Lab.Application.Packages;

/// <summary>
/// Represents the PATH environment variable.
/// </summary>
internal sealed class EnvironmentPath
{
    private const string Variable = "PATH";

    /// <summary>
    /// Puts <paramref name="directory"/> first on the path.
    /// </summary>
    public void Prepend(IDirectory directory) =>
        Environment.SetEnvironmentVariable(Variable, $"{directory.AbsolutePath}{Path.PathSeparator}{Environment.GetEnvironmentVariable(Variable)}");
}
