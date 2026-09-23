using Wolfe.Lab.Build.Workflows.CaddyCertificates.Jobs;

namespace Wolfe.Lab.Build.Workflows.CaddyCertificates;

/// <summary>
/// The front door's certificate: <c>"workflow": "caddy-certificates"</c>.
/// </summary>
public sealed class CaddyCertificatesWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "caddy-certificates";

    /// <inheritdoc />
    public string Label => "caddy certificates";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new RenewJob()];
}
