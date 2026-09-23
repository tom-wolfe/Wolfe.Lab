namespace Wolfe.Lab.Build.Clients.Gatus;

/// <summary>
/// What Gatus's health endpoint answered.
/// </summary>
/// <param name="Status">The status as Gatus spells it; <c>UP</c> when it is serving.</param>
public sealed record GatusStatus(string Status)
{
    /// <summary>
    /// Whether Gatus reports itself up.
    /// </summary>
    public bool IsUp => Status == "UP";
}
