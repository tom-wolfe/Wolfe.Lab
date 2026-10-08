namespace Wolfe.Lab.Domain.Catalog.Components.Garage;

/// <summary>
/// A Garage node's role in its cluster's layout.
/// </summary>
/// <param name="Zone">The zone it stands in.</param>
/// <param name="Capacity">How much it stores.</param>
public sealed record GarageLayout(GarageZone Zone, StorageCapacity Capacity);
