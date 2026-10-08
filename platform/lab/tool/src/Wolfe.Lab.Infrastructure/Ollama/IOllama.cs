using Wolfe.Lab.Domain.Catalog.Components.Models;

namespace Wolfe.Lab.Infrastructure.Ollama;

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
    /// What a model on the node is made of, or null when the node does not hold it.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<OllamaModelfile?> Describe(OllamaModel model, CancellationToken ct = default);

    /// <summary>
    /// Makes <paramref name="name"/> a model built from <paramref name="from"/>, with
    /// <paramref name="context"/> when given: a manifest, sharing the weights, replacing whatever
    /// the name was.
    /// </summary>
    /// <param name="name">The name to make.</param>
    /// <param name="from">The model it is built from.</param>
    /// <param name="context">The context it runs with; the model's own when null.</param>
    /// <param name="ct">The cancellation token.</param>
    Task Create(OllamaModel name, OllamaModel from, ContextLength? context, CancellationToken ct = default);

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

    /// <summary>
    /// Waits for a server that has just started to answer.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True when the server started successfully, otherwise false.</returns>
    Task<bool> AwaitServing(CancellationToken ct = default);
}
