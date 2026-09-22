namespace Wolfe.Lab.Build.Clients.Chezmoi;

/// <summary>
/// Renders for real — a render writes only scratch directories, and the check is the rehearsal —
/// and skips the apply.
/// </summary>
internal sealed class DryRunChezmoi(IWorkflowLog log, ChezmoiClient inner) : IChezmoi
{
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> Render(IDirectory source, string profile, IDirectory destination, CancellationToken ct = default) =>
        inner.Render(source, profile, destination, ct);

    /// <inheritdoc />
    public Task Update(CancellationToken ct = default)
    {
        log.Skipped("Would run chezmoi update on this node.");
        return Task.CompletedTask;
    }
}
