using Wolfe.Lab.Application.Workflows.CaddyCertificates.Jobs;

namespace Wolfe.Lab.Application.Workflows.CaddyCertificates;

/// <summary>
/// The front door's certificate: <c>"workflow": "caddy-certificates"</c>.
/// </summary>
public sealed class CaddyCertificatesWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "caddy-certificates";

    /// <inheritdoc />
    public override string Label => "caddy certificates";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new RenewJob()];
}
