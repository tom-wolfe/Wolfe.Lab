using Wolfe.Lab.Domain.Catalog.Components.Garage;

namespace Wolfe.Lab.Infrastructure.Garage;

/// <summary>
/// A cluster's layout as Garage reports it: the version applied, and the storage role each node has.
/// </summary>
/// <param name="Version">The version applied; zero before any has been.</param>
/// <param name="Roles">Each storing node's role, by its id; a gateway, which stores nothing, has none.</param>
public sealed record ClusterLayout(int Version, IReadOnlyDictionary<string, GarageLayout> Roles)
{
    /// <summary>
    /// <paramref name="nodeId"/>'s role, or null while it has none.
    /// </summary>
    public GarageLayout? RoleOf(string nodeId) => Roles.GetValueOrDefault(nodeId);
}
