using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Infrastructure.Ollama;

namespace Wolfe.Lab.Application.Workflows.Ollama.Models;

/// <summary>
/// What this node's server serves: each model by its use, or nothing when the node runs no server
/// of the service.
/// </summary>
/// <param name="Server">The server on this node, if any.</param>
/// <param name="Served">What it serves of this deployment's models.</param>
/// <param name="Uses">The name of every use the service declares, deployed here or not: a name the
/// lab gives that is none of them is a use retired.</param>
public sealed record ServerPlan(ComponentName? Server, IReadOnlyList<ServedModel> Served, IReadOnlyList<OllamaModel> Uses)
{
    private const string Namespace = "lab/";

    /// <summary>
    /// Nothing to serve here.
    /// </summary>
    public static ServerPlan None { get; } = new(null, [], []);

    /// <summary>
    /// The models the server runs, once each.
    /// </summary>
    public IReadOnlyList<OllamaModel> Models => [.. Served.Select(served => served.Model).Distinct()];

    /// <summary>
    /// Whether <paramref name="model"/> is a name the lab gives a use, rather than a model.
    /// </summary>
    public static bool IsUse(OllamaModel model) => model.Value.StartsWith(Namespace, StringComparison.Ordinal);
}
