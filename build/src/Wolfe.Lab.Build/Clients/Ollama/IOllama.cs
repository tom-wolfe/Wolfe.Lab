
namespace Wolfe.Lab.Build.Clients.Ollama;

/// <summary>
/// The model server, as the lab drives it.
/// </summary>
public interface IOllama
{
    /// <summary>
    /// The models the node already holds.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    Task<IReadOnlyList<OllamaModel>> Installed(CancellationToken ct = default);

    /// <summary>
    /// Fetches a model the node does not have.
    /// </summary>
    /// <param name="model">The model to pull.</param>
    /// <param name="ct">The cancellation token.</param>
    Task Pull(OllamaModel model, CancellationToken ct = default);

    /// <summary>
    /// What each model on the node is, by the id ollama lists it with.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    Task<IReadOnlyDictionary<OllamaModel, string>> Identities(CancellationToken ct = default);

    /// <summary>
    /// Points a second name at a model the node holds, replacing whatever that name pointed at.
    /// </summary>
    /// <param name="source">The model to name.</param>
    /// <param name="destination">The name to give it.</param>
    /// <param name="ct">The cancellation token.</param>
    Task Copy(OllamaModel source, OllamaModel destination, CancellationToken ct = default);

    /// <summary>
    /// Removes a name. Layers another name still uses stay.
    /// </summary>
    /// <param name="model">The name to remove.</param>
    /// <param name="ct">The cancellation token.</param>
    Task Remove(OllamaModel model, CancellationToken ct = default);

    /// <summary>
    /// Whether the server is answering yet.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    Task<bool> IsServing(CancellationToken ct = default);
}
