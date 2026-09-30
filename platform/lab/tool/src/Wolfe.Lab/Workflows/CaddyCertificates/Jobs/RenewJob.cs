using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Clients.Caddy.Steps;
using Wolfe.Lab.Clients.Gates.Steps;
using Wolfe.Lab.Workflows.CaddyCertificates.Models;
using Wolfe.Lab.Workflows.CaddyCertificates.Steps;

namespace Wolfe.Lab.Workflows.CaddyCertificates.Jobs;

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
        Step.FromType<EnsureCertificateStore>(),
        Step.FromType<GateApproval>(),
        Step.FromType<IssueCertificate>(),
        Step.FromType<ReloadCaddy>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<CaddyCertificatesOptions> options) => options
        .Require(s => s.Caddy.ToInstance() is not null, "'caddy.container' and 'caddy.caddyfile' must both be set in ritten.json: the caddy this reloads.")
        .Require(s => s.Image is { Length: > 0 }, "'image' not set in ritten.json.")
        .Require(s => s.Email is { Length: > 0 }, "'email' not set in ritten.json.")
        .Require(s => s.Domains.Count > 0, "'domains' names nothing in ritten.json.")
        .Require(s => s.Dns is { Length: > 0 }, "'dns' not set in ritten.json.")
        .Require(s => s.Store is not null, "'store' not set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, CaddyCertificatesOptions options)
    {
        base.Configure(builder, options);
        builder.AddDocker();
        if (options.Caddy.ToInstance() is { } caddy)
        {
            builder.Services.AddSingleton(caddy);
        }
        if (options.ToRequest() is { } request)
        {
            builder.Services.AddSingleton(request);
        }
    }
}
