namespace Wolfe.Lab.Domain.Catalog.Nodes;

/// <summary>
/// What a node is to the lab (README.md, "Nodes"): what fails when it does.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
[Instance("Server", "server", "Always on: its failure is an outage, and it is watched as such.")]
[Instance("Hybrid", "hybrid", "A workstation that serves while it is on; nothing may depend on it.")]
public readonly partial struct NodeRole : IClosedSet<NodeRole>
{
    /// <inheritdoc />
    public static IReadOnlyList<NodeRole> All => [Server, Hybrid];

    private static Validation Validate(string input) =>
        All.Any(role => role.Value == input)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not a node's role ({string.Join(", ", All)}).");
}
