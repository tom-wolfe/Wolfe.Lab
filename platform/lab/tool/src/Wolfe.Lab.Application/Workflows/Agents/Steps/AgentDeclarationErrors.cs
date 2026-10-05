using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.Agents.Steps;

/// <summary>
/// The well-known problems with finding what a node runs of an agents component.
/// </summary>
internal static class AgentDeclarationErrors
{
    /// <summary>
    /// The environment names no node the catalog declares as the one the deploy runs on.
    /// </summary>
    public static Error NotANode(string? given) => given is null
        ? new Error($"{LabNode.Variable} is not set, so this is no node of the lab's: a deploy runs on the node it converges.")
        : new Error($"{LabNode.Variable} is '{given}', which platform/nodes.yaml does not declare.");

    /// <summary>
    /// The node declares it keeps the lab somewhere other than where this run's environment says.
    /// </summary>
    public static Error DirectoriesDiffer(Node node, LabDirectories declared, LabDirectories environment) =>
        new($"{node} keeps the lab in {declared.Root.AbsolutePath} and {declared.Data.AbsolutePath} by platform/nodes.yaml, "
            + $"but {LabDirectories.RootVariable} and LAB_DATA here say {environment.Root.AbsolutePath} and {environment.Data.AbsolutePath}: make them one place.");

    /// <summary>
    /// A variable the node sets for every agent, declared by the component as well.
    /// </summary>
    public static Error SetByTheNode(string variable) => new($"{variable} is set for every agent by its node, and is not declared.");
}
