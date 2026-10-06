using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>agents</c> workflow operates: a host process running on a node.
/// </summary>
[AdditionalProperties(false)]
internal sealed record AgentsDocument : ComponentDocument
{
    [Required, Description("The nodes it runs on: 'all', 'every <role>', or a list of nodes by name.")]
    public required DeploymentTargetDocument RunsOn { get; init; }

    [Required, Pattern(LabSchema.NamePattern), Description("The name it runs under on each node — its unit is dev.twolfe.<agent> — and the service_name its logs carry.")]
    public string Agent { get; init; } = "";

    [Description("What it runs, installed from a GitHub release.")]
    public AgentPackageDocument? Package { get; init; }

    [Required, MinLength(1), Description("The executable: {package}/<file> for one its package holds.")]
    public string Program { get; init; } = "";

    [Description("The arguments it runs with.")]
    public List<string>? Arguments { get; init; }

    [Description("The variables it runs with; what the node sets for every agent is not declared.")]
    public Dictionary<string, string>? Environment { get; init; }

    [UniqueItems(true), Description("Units an earlier supervisor ran it under, retired before it starts.")]
    public List<string>? Supersedes { get; init; }
}
