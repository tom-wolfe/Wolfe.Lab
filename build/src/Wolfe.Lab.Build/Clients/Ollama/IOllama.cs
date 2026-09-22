
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
}
