using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Workflows.Caddy.Models;
using Wolfe.Lab.Build.Workflows.Caddy.Steps;
using Wolfe.Lab.Build.Workflows.Common.Steps;

namespace Wolfe.Lab.Build.Workflows.Caddy.Jobs;

/// <summary>
/// Issues or renews the wildcard certificate and hands it to the running caddy.
/// </summary>
/// <remarks>
/// Nightly by workflow, and once before caddy's first start, because caddy loads the certificate
/// from files and cannot start without them. SSL maintenance lives here rather than in caddy: the
/// caddy DNS module for Netlify is dead upstream, lego's in-tree provider is maintained, and a
/// renewal that fails is a red run instead of a log line nobody reads.
/// </remarks>
internal sealed class RenewCertsJob : LabJob<CaddySettings>
{
    public override string Name => "renew-certs";

    public override string Description => "Issues or renews the wildcard certificate with lego and reloads caddy.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<EnsureCertificateStore>(),
        Step.FromType<GateApproval>(),
        Step.FromType<IssueCertificate>(),
        Step.FromType<ReloadCaddy>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<CaddySettings> settings) => settings
        .Require(s => s.Certificate.Image is { Length: > 0 }, "'certificate.image' not set in ritten.json.")
        .Require(s => s.Certificate.Email is { Length: > 0 }, "'certificate.email' not set in ritten.json.")
        .Require(s => s.Certificate.Domains.Count > 0, "'certificate.domains' names nothing in ritten.json.")
        .Require(s => s.Certificate.Dns is { Length: > 0 }, "'certificate.dns' not set in ritten.json.")
        .Require(s => s.Certificate.Store is not null, "'certificate.store' not set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, CaddySettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker();
        if (settings.Certificate.ToRequest() is { } request)
        {
            builder.Services.AddSingleton(request);
        }
    }
}
