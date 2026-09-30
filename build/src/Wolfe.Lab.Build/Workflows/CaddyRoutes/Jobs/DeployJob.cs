using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Clients.Caddy.Steps;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Clients.Releases.Steps;
using Wolfe.Lab.Build.Workflows.CaddyRoutes.Models;
using Wolfe.Lab.Build.Workflows.CaddyRoutes.Steps;

namespace Wolfe.Lab.Build.Workflows.CaddyRoutes.Jobs;

/// <summary>
/// Republishes every component's route and tells the running caddy to read them.
/// </summary>
/// <remarks>
/// Snippets arrive through a bind mount and never change compose's config hash, so the door
/// has to be told; a plain redeploy of the compose component would notice nothing.
/// </remarks>
internal sealed class DeployJob : LabJob<CaddyRoutesOptions>
{
    public override string Name => "deploy";

    public override string Description => "Gathers every component's route snippet, installs them beside the front door and reloads it.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatherRoutes>(),
        Step.FromType<ResolveRelease>(),
        Step.FromType<GateApproval>(),
        Step.FromType<InstallRoutes>(),
        Step.FromType<ReloadCaddy>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<CaddyRoutesOptions> options) => options
        .Require(s => s.Caddy.ToInstance() is not null, "'caddy.container' and 'caddy.caddyfile' must both be set in ritten.json: the caddy this reloads.")
        .Require(s => s.Release is { Length: > 0 }, "'release' not set in ritten.json: the directory the Caddyfile imports routes from.");

    protected override void Configure(IWorkflowBuilder builder, CaddyRoutesOptions options)
    {
        base.Configure(builder, options);
        builder.AddDocker();
        if (options.Caddy.ToInstance() is { } caddy)
        {
            builder.Services.AddSingleton(caddy);
        }
        if (options.Release is { Length: > 0 } release)
        {
            builder.AddReleases(release);
        }
    }
}
