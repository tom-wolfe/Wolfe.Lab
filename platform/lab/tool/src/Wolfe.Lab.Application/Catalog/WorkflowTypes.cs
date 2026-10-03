using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Application.Catalog;

/// <summary>
/// Maps Ritten workflows to component types.
/// </summary>
internal static class WorkflowTypes
{
    private static readonly Dictionary<string, ComponentType> Runs = new(StringComparer.Ordinal)
    {
        ["docker"] = ComponentType.Compose,
        ["dotnet-service"] = ComponentType.Compose,
        ["agents"] = ComponentType.Agent,
        ["ollama"] = ComponentType.Agent,
        ["backup"] = ComponentType.Snapshot,
        ["obsidian"] = ComponentType.Git,
        ["forgejo-runners"] = ComponentType.HostRunner,
        ["restic"] = ComponentType.Restic,
        ["tofu"] = ComponentType.Tofu,
        ["caddy-certificates"] = ComponentType.Acme,
        ["image"] = ComponentType.Image,
        ["dotnet-tool"] = ComponentType.NuGet,
        ["chezmoi"] = ComponentType.Chezmoi
    };

    /// <summary>
    /// The type a component running <paramref name="workflow"/> is, or why it is none: the
    /// workflow becomes a facet of something else, or goes.
    /// </summary>
    public static Result<ComponentType> Of(string workflow) =>
        Runs.TryGetValue(workflow, out var type)
            ? type
            : new Error($"the {workflow} workflow becomes a facet of what it serves, or goes, rather than a component of its own (ROADMAP.md #14).");
}
