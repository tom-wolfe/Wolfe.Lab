namespace Wolfe.Lab.Domain.Catalog.Nodes;

/// <summary>
/// The well-known problems with a node's declaration, and with placing a component on nodes.
/// </summary>
public static class NodeErrors
{
    /// <summary>
    /// The declaration is not in <c>platform/</c>, where the lab's nodes are declared.
    /// </summary>
    public static Error OutOfPlace { get; } = new("a node is declared in platform/, beside the lab's other machinery.");

    /// <summary>
    /// Another declaration names a node the same.
    /// </summary>
    public static Error DeclaredAlready(NodeName name, DocumentSource other) => new($"the node '{name}' is declared already, in {other}.");

    /// <summary>
    /// Another node carries the same runner; a deploy finds its node by its runner.
    /// </summary>
    public static Error RunnerShared(string runner, DocumentSource other) =>
        new($"another node carries the runner '{runner}' too ({other}); a deploy finds its node by its runner.");

    /// <summary>
    /// A placement names a node the lab does not declare.
    /// </summary>
    public static Error Undeclared(NodeName name) => new($"names the node '{name}', which the lab does not declare.");

    /// <summary>
    /// A placement that takes in no node at all.
    /// </summary>
    public static Error PlacedNowhere(Placement placement) => new($"'{placement}' takes in no node the lab declares.");

    /// <summary>
    /// A placement's rule is neither every node nor every node of a role.
    /// </summary>
    public static Error NotARule(string rule) =>
        new($"'{rule}' is not a placement: 'every node', 'every <role>' ({string.Join(", ", NodeRole.All)}), or a list of nodes.");

    /// <summary>
    /// A placement's list is empty or names a node twice.
    /// </summary>
    public static Error NotAList { get; } = new("a placement's list names each node once, and at least one.");
}
