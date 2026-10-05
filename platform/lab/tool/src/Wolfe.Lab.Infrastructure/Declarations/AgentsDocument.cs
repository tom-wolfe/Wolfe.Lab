using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>agents</c> workflow operates, as its file writes it: one host process,
/// placed on the nodes it runs on.
/// </summary>
/// <remarks>
/// While its directory's <c>ritten.json</c> still declares its agents per node, a component
/// declares none of this, and the deploy reads them from there; once it declares
/// <c>runsOn</c>, its <c>agent</c> and <c>program</c> come with it, and <c>ritten.json</c>
/// declares no nodes.
/// </remarks>
[AdditionalProperties(false)]
internal sealed record AgentsDocument : ComponentDocument
{
    [Description("The nodes it runs on: 'all', 'every <role>', or a list of nodes by name.")]
    public DeploymentTargetDocument? RunsOn { get; init; }

    [Pattern(LabSchema.NamePattern), Description("The name it runs under on each node — its unit is dev.twolfe.<agent> — and the service_name its logs carry.")]
    public string? Agent { get; init; }

    [Description("What it runs, installed from a GitHub release.")]
    public AgentPackageDocument? Package { get; init; }

    [MinLength(1), Description("The executable: {package}/<file> for one its package holds.")]
    public string? Program { get; init; }

    [Description("The arguments it runs with.")]
    public List<string>? Arguments { get; init; }

    [Description("The variables it runs with; what the node sets for every agent is not declared.")]
    public Dictionary<string, string>? Environment { get; init; }

    [UniqueItems(true), Description("Units an earlier supervisor ran it under, retired before it starts.")]
    public List<string>? Supersedes { get; init; }

    /// <summary>
    /// Whether it declares anything of its agent, rather than leaving it to ritten.json.
    /// </summary>
    public bool DeclaresAgent => RunsOn is not null || Agent is not null || Package is not null || Program is not null
                                 || Arguments is not null || Environment is not null || Supersedes is not null;
}
