namespace Wolfe.Lab.Domain.Catalog.Components.Agents;

/// <summary>
/// Represents an agent or a daemon that the runs on a node.
/// </summary>
public sealed record AgentProcess
{
    /// <summary>
    /// The name it runs under on the node.
    /// </summary>
    public required AgentName Name { get; init; }

    /// <summary>
    /// What it runs, when the lab installs it rather than the node having it.
    /// </summary>
    public AgentPackage? Package { get; init; }

    /// <summary>
    /// The executable: <c>{package}/alloy-{platform}</c>.
    /// </summary>
    public required Template Program { get; init; }

    /// <summary>
    /// The arguments it runs with.
    /// </summary>
    public IReadOnlyList<Template> Arguments { get; init; } = [];

    /// <summary>
    /// The variables it runs with, beyond those the host sets for every agent.
    /// </summary>
    public IReadOnlyDictionary<string, Template> Environment { get; init; } = new Dictionary<string, Template>();

    /// <summary>
    /// Units an earlier supervisor ran it under, retired before it starts.
    /// </summary>
    public IReadOnlyList<string> Supersedes { get; init; } = [];

    /// <summary>
    /// Every value it holds that a placeholder may be written in, by the field it is in.
    /// </summary>
    internal IEnumerable<(string Field, Template Value)> Values =>
    [
        ("program", Program),
        .. Arguments.Select((argument, index) => ($"arguments.{index}", argument)),
        .. Environment.Select(variable => ($"environment.{variable.Key}", variable.Value))
    ];

    /// <summary>
    /// Every package value a placeholder may be written in, by the field it is in.
    /// </summary>
    internal IEnumerable<(string Field, Template Value)> PackageValues => Package != null
        ? [("package.asset", Package.Asset), .. Package.Checksums is { } checksums ? [("package.checksums", checksums)] : Array.Empty<(string, Template)>()]
        : [];
}
