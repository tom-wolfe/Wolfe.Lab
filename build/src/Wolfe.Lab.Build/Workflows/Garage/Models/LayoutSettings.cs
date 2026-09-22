namespace Wolfe.Lab.Build.Workflows.Garage.Models;

/// <summary>
/// The <c>layout</c> section: the zone this node is in and how much it stores.
/// </summary>
public sealed record LayoutSettings
{
    /// <summary>
    /// The zone.
    /// </summary>
    public string? Zone { get; init; }

    /// <summary>
    /// The capacity, as Garage spells it (<c>500G</c>).
    /// </summary>
    public string? Capacity { get; init; }

    /// <summary>
    /// The layout the job applies, or null while a field is missing.
    /// </summary>
    public Layout? ToLayout() => Zone is { Length: > 0 } zone && Capacity is { Length: > 0 } capacity ? new Layout(zone, capacity) : null;
}
