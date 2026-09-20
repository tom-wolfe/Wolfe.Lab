using Wolfe.Lab.Build.Ollama.Models;

namespace Wolfe.Lab.Build.Ollama;

/// <summary>
/// Says which models would be fetched. Reading what the node holds is safe and still happens,
/// so a rehearsal reports the real difference rather than listing everything declared.
/// </summary>
internal sealed class DryRunOllama(IWorkflowLog log, OllamaClient inner) : IOllama
{
    /// <inheritdoc />
    public Task<IReadOnlyList<OllamaModel>> Installed(CancellationToken ct = default) => inner.Installed(ct);

    /// <inheritdoc />
    public Task Pull(OllamaModel model, CancellationToken ct = default)
    {
        log.Skipped($"Would pull {model.Value}.");
        return Task.CompletedTask;
    }
}
