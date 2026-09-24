
namespace Wolfe.Lab.Build.Clients.Ollama;

/// <summary>
/// Says which models would be fetched. Reading what the node holds is safe and still happens,
/// so a rehearsal reports the real difference rather than listing everything declared.
/// </summary>
internal sealed class DryRunOllama(IWorkflowLog log, OllamaClient inner) : IOllama
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<OllamaModel>> Installed(CancellationToken ct = default)
    {
        if (!await inner.IsServing(ct))
        {
            log.Detail("The server is not running here yet; every declared model would be pulled.");
            return [];
        }

        return await inner.Installed(ct);
    }

    /// <inheritdoc />
    public Task<bool> IsServing(CancellationToken ct = default) => inner.IsServing(ct);

    /// <inheritdoc />
    public Task Pull(OllamaModel model, CancellationToken ct = default)
    {
        log.Skipped($"Would pull {model.Value}.");
        return Task.CompletedTask;
    }
}
