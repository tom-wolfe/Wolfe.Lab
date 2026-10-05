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
    /// A deployment target names a node the lab does not declare.
    /// </summary>
    public static Error Undeclared(NodeName name) => new($"names the node '{name}', which the lab does not declare.");

    /// <summary>
    /// A deployment target that takes in no node at all.
    /// </summary>
    public static Error EmptyTarget(DeploymentTarget target) => new($"'{target}' takes in no node the lab declares.");

    /// <summary>
    /// A deployment target's rule is neither all nodes nor every node of a role.
    /// </summary>
    public static Error NotARule(string rule) =>
        new($"'{rule}' is not a deployment target: 'all', 'every <role>' ({string.Join(", ", NodeRole.All)}), or a list of nodes.");

    /// <summary>
    /// A deployment target's list is empty or names a node twice.
    /// </summary>
    public static Error NotAList { get; } = new("a deployment target's list names each node once, and at least one.");
}
