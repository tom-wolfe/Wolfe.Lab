using Wolfe.Lab.Build.Clients.Ollama;

namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// The role names a deploy will point, each at the model that fills it on this node.
/// </summary>
/// <param name="Roles">Each role's alias, and the model it points at.</param>
public sealed record RolePlan(IReadOnlyDictionary<OllamaModel, OllamaModel> Roles)
{
    /// <summary>
    /// The namespace every role's alias lives under, so a role can never shadow a real model.
    /// </summary>
    internal const string Namespace = "lab/";

    /// <summary>
    /// The plan a component's declared roles make.
    /// </summary>
    /// <param name="roles">The <c>models.roles</c> section.</param>
    public static RolePlan From(IReadOnlyDictionary<string, ModelRole> roles) =>
        new(roles
            .Where(role => role.Value.Model is not null)
            .OrderBy(role => role.Key, StringComparer.Ordinal)
            .ToDictionary(role => Alias(role.Key), role => role.Value.Model!.Value));

    /// <summary>
    /// The name a caller asks for: <c>lab/background</c>, which ollama stores as
    /// <c>lab/background:latest</c>. Untagged on purpose — a role is a moving pointer, which
    /// is exactly what <c>latest</c> means and exactly what a pinned model must not be.
    /// </summary>
    /// <param name="role">The role's name.</param>
    public static OllamaModel Alias(string role) => OllamaModel.From($"{Namespace}{role}:latest");

    /// <summary>
    /// Whether a model on the node is a role's alias rather than a model somebody pulled.
    /// </summary>
    /// <param name="model">The model to test.</param>
    public static bool IsAlias(OllamaModel model) => model.Value.StartsWith(Namespace, StringComparison.Ordinal);
}
