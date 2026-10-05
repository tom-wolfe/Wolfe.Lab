using Microsoft.Extensions.Configuration;
using Wolfe.Lab.Domain.Catalog.Nodes;

namespace Wolfe.Lab.Infrastructure.Releases;

/// <summary>
/// Which node the lab is currently running on.
/// </summary>
public sealed class LabNode
{
    /// <summary>
    /// The variable naming the node.
    /// </summary>
    public const string Variable = "LAB_NODE";

    /// <summary>
    /// The node's name as the environment gives it, or null when it gives none.
    /// </summary>
    public string? Given { get; set; }

    /// <summary>
    /// The node's name, when what the environment gives is one.
    /// </summary>
    public NodeName? Name => Given is { } given && NodeName.TryFrom(given) is { IsSuccess: true } name ? name.ValueObject : null;

    /// <summary>
    /// Takes the node's name from <paramref name="configuration"/>, which reads the environment
    /// with its prefix stripped: <c>LAB_NODE</c> is <c>NODE</c> there.
    /// </summary>
    internal void Configure(IConfiguration configuration)
    {
        if (configuration[Variable[LabConfiguration.EnvironmentPrefix.Length..]] is { Length: > 0 } node)
        {
            Given = node;
        }
    }
}
