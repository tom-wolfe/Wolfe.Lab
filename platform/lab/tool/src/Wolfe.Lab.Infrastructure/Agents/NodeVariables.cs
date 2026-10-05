using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Infrastructure.Agents;

/// <summary>
/// The variables a node sets for every agent it runs: which node it is, the node's role, and the
/// install root — so an agent's config can say where it is without any agent declaring it.
/// </summary>
public static class NodeVariables
{
    /// <summary>
    /// The node's name.
    /// </summary>
    public const string Host = "LAB_HOST";

    /// <summary>
    /// The node's role.
    /// </summary>
    public const string Role = "LAB_ROLE";

    /// <summary>
    /// Every variable the node sets, by name.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } = [Host, Role, LabDirectories.RootVariable];

    /// <summary>
    /// Their values on <paramref name="node"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ForNode(Node node, LabDirectories directories) => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Host] = node.Name.Value,
        [Role] = node.Role.Value,
        [LabDirectories.RootVariable] = directories.Root.AbsolutePath
    };
}
