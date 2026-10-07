using Wolfe.Lab.Domain.Catalog.Nodes;

namespace Wolfe.Lab.Domain.Catalog.Components.Agents;

/// <summary>
/// The well-known problems with an agents component's declaration, and with expanding it on a node.
/// </summary>
public static class AgentErrors
{
    /// <summary>
    /// A placeholder the field cannot hold.
    /// </summary>
    public static Error UnknownPlaceholder(string placeholder, IEnumerable<string> known) =>
        new($"{{{placeholder}}} is not a placeholder here ({string.Join(", ", known.Select(name => $"{{{name}}}"))}, or {{node.<name>.address}}).");

    /// <summary>
    /// The node it is expanded on has nothing for a placeholder.
    /// </summary>
    public static Error NothingFor(string placeholder, Node node) => new($"{{{placeholder}}} has no value on {node}.");
}
