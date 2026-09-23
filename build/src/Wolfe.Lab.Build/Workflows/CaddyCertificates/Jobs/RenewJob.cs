using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Clients.Caddy.Steps;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.CaddyCertificates.Models;
using Wolfe.Lab.Build.Workflows.CaddyCertificates.Steps;

namespace Wolfe.Lab.Build.Workflows.CaddyCertificates.Jobs;

/// <summary>
/// Issues or renews the wildcard certificate and hands it to the running caddy.
/// </summary>
/// <remarks>
/// Nightly by workflow, and once before caddy's first start, because caddy loads the certificate
/// from files and cannot start without them. SSL maintenance lives here rather than in caddy: the
/// caddy DNS module for Netlify is dead upstream, lego's in-tree provider is maintained, and a
/// renewal that fails is a red run instead of a log line nobody reads.
/// </remarks>
internal sealed class RenewJob : LabJob<CaddyCertificatesSettings>
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

    protected override void ValidateSettings(SettingsValidator<CaddyCertificatesSettings> settings) => settings
        .Require(s => s.Image is { Length: > 0 }, "'image' not set in ritten.json.")
        .Require(s => s.Email is { Length: > 0 }, "'email' not set in ritten.json.")
        .Require(s => s.Domains.Count > 0, "'domains' names nothing in ritten.json.")
        .Require(s => s.Dns is { Length: > 0 }, "'dns' not set in ritten.json.")
        .Require(s => s.Store is not null, "'store' not set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, CaddyCertificatesSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker();
        if (settings.ToRequest() is { } request)
        {
            builder.Services.AddSingleton(request);
        }
    }
}
