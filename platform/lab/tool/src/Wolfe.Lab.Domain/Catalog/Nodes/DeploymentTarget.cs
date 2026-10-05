namespace Wolfe.Lab.Domain.Catalog.Nodes;

/// <summary>
/// Describes the nodes a component is deployed to.
/// </summary>
public sealed class DeploymentTarget
{
    private const string Every = "every ";
    private const string AllNodes = "all";

    private readonly NodeRole? _role;
    private readonly bool _everyNode;

    private DeploymentTarget(bool everyNode, NodeRole? role, IReadOnlyList<NodeName> named)
    {
        _everyNode = everyNode;
        _role = role;
        Named = named;
    }

    /// <summary>
    /// On every node the lab declares: <c>all</c>.
    /// </summary>
    public static DeploymentTarget All { get; } = new(true, null, []);

    /// <summary>
    /// The nodes it names, when it names them rather than a rule.
    /// </summary>
    public IReadOnlyList<NodeName> Named { get; }

    /// <summary>
    /// On every node of <paramref name="role"/>.
    /// </summary>
    public static DeploymentTarget EveryOf(NodeRole role) => new(false, role, []);

    /// <summary>
    /// A rule as written: <c>all</c>, or <c>every &lt;role&gt;</c>.
    /// </summary>
    public static Result<DeploymentTarget> Rule(string rule) =>
        rule == AllNodes ? All
        : rule.StartsWith(Every, StringComparison.Ordinal) && NodeRole.TryFrom(rule[Every.Length..]) is { IsSuccess: true } role ? EveryOf(role.ValueObject)
        : NodeErrors.NotARule(rule);

    /// <summary>
    /// On the nodes it names, each once.
    /// </summary>
    public static Result<DeploymentTarget> On(IReadOnlyList<NodeName> nodes) =>
        nodes.Count > 0 && nodes.Distinct().Count() == nodes.Count
            ? new DeploymentTarget(false, null, nodes)
            : NodeErrors.NotAList;

    /// <summary>
    /// Whether it takes in <paramref name="node"/>.
    /// </summary>
    public bool Includes(Node node) => _everyNode || node.Role == _role || Named.Contains(node.Name);

    /// <summary>
    /// The nodes it takes in, of those the catalog declares.
    /// </summary>
    public IReadOnlyList<Node> In(ServiceCatalog catalog) => [.. catalog.Nodes.Where(Includes)];

    /// <inheritdoc />
    public override string ToString() =>
        _everyNode ? AllNodes : _role is { } role ? Every + role : $"[{string.Join(", ", Named)}]";
}
