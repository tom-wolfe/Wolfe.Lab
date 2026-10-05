using Wolfe.Lab.Domain.Network;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog.Nodes;

/// <summary>
/// Represents a machine the lab runs on.
/// </summary>
public sealed class Node : IEquatable<Node>
{
    private Node() { }

    /// <summary>
    /// Where it is declared.
    /// </summary>
    public required DocumentSource Source { get; init; }

    /// <summary>
    /// Its name.
    /// </summary>
    public required NodeName Name { get; init; }

    /// <summary>
    /// What it is to the lab.
    /// </summary>
    public required NodeRole Role { get; init; }

    /// <summary>
    /// Its operating system and architecture.
    /// </summary>
    public required NodePlatform Platform { get; init; }

    /// <summary>
    /// Where the other nodes reach it: its name on the tailnet.
    /// </summary>
    public required HostName Address { get; init; }

    /// <summary>
    /// Where it keeps the lab.
    /// </summary>
    public required NodeDirectories Directories { get; init; }

    /// <summary>
    /// Where its Docker socket is, when it runs Docker: <c>unix:///var/run/docker.sock</c>.
    /// </summary>
    public DockerHost? Docker { get; set; }

    /// <summary>
    /// The external drives it holds, mounted where it mounts them.
    /// </summary>
    public IReadOnlyList<HostPath> Drives { get; set; } = [];

    /// <summary>
    /// Creates a new node.
    /// </summary>
    public static Result<Node> Create(DocumentSource source, NodeName name, NodeRole role, NodePlatform platform, HostName address, NodeDirectories directories) =>
        source.Directories is ["platform"]
            ? new Node { Source = source, Name = name, Role = role, Platform = platform, Address = address, Directories = directories }
            : CatalogError.In(source, NodeErrors.OutOfPlace);

    /// <inheritdoc />
    public bool Equals(Node? other) => other is not null && Name == other.Name;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Node);

    /// <inheritdoc />
    public override int GetHashCode() => Name.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => Name.Value;
}
