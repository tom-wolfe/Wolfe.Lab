using Wolfe.Lab.Build.Clients.Ollama;

namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// One entry of <c>models.roles</c>: the model this node answers with when a caller asks for
/// the role rather than for a model.
/// </summary>
public sealed record ModelRole
{
    /// <summary>
    /// The declared model that fills the role here.
    /// </summary>
    public OllamaModel? Model { get; init; }

    /// <summary>
    /// Whether every component of the slice must declare this role with this same model.
    /// </summary>
    /// <remarks>
    /// Most roles are best-fit on purpose — the Studio's <c>background</c> is a bigger model
    /// than the mini's. An embedding cannot be: vectors from two models are not comparable,
    /// and an index built on one node and searched on the other returns nonsense without
    /// failing.
    /// </remarks>
    public bool Identical { get; init; }
}
