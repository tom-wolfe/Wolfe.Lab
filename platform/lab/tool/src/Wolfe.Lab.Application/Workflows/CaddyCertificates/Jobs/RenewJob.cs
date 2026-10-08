using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Application.Caddy;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Workflows.CaddyCertificates.Models;
using Wolfe.Lab.Application.Workflows.CaddyCertificates.Steps;

namespace Wolfe.Lab.Application.Workflows.CaddyCertificates.Jobs;

/// <summary>
/// Issues or renews the wildcard certificate and hands it to the running caddy.
/// </summary>
/// <remarks>
/// Nightly by workflow, and once before caddy's first start, because caddy loads the certificate
/// from files and cannot start without them. SSL maintenance lives here rather than in caddy: the
/// caddy DNS module for Netlify is dead upstream, lego's in-tree provider is maintained, and a
/// renewal that fails is a red run instead of a log line nobody reads.
/// </remarks>
internal sealed class RenewJob : LabJob<CaddyCertificatesOptions>
{
    public override string Name => "renew";

    public override string Description => "Issues or renews the wildcard certificate with lego and reloads caddy.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveCertificate>(),
        Step.FromType<ResolveCaddy>(),
        Step.FromType<EnsureCertificateStore>(),
        Step.FromType<GateApproval>(),
        Step.FromType<IssueCertificate>(),
        Step.FromType<ReloadCaddy>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    public override bool RequiresProject => false;

    protected override void Configure(IWorkflowBuilder builder, CaddyCertificatesOptions options)
    {
        base.Configure(builder, options);
        builder.AddDocker();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(options.Caddy);
    }
}
