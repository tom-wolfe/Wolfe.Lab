namespace Wolfe.Lab.Build.Workflows.GarageLayout.Models;

/// <summary>
/// The shape of the layout component's <c>ritten.json</c>: <c>"workflow": "garage-layout"</c>.
/// </summary>
public sealed record GarageLayoutSettings : WorkflowSettings
{
    /// <summary>
    /// The zone the one node is placed in.
    /// </summary>
    public string? Zone { get; init; }

    /// <summary>
    /// The capacity it is assigned, as garage spells it (<c>500G</c>).
    /// </summary>
    public string? Capacity { get; init; }

    /// <summary>
    /// The layout as the steps apply it, or null while a field is missing.
    /// </summary>
    public Layout? ToLayout() => Zone is { Length: > 0 } zone && Capacity is { Length: > 0 } capacity ? new Layout(zone, capacity) : null;
}
