namespace Wolfe.Lab.Build.Clients.Agents.Launchd;

/// <summary>
/// What launchd is allowed, from <c>Launchd:Unloading</c> in <c>appsettings.json</c>.
/// </summary>
public sealed class LaunchdOptions
{
    /// <summary>
    /// Added to an agent's own exit timeout for the wait on its unload, for launchd to notice.
    /// </summary>
    public TimeSpan Margin { get; set; }
}
