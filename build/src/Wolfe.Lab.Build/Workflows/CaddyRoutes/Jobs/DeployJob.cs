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
internal sealed class DeployJob : LabJob<CaddyRoutesSettings>
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

    protected override void ValidateSettings(SettingsValidator<CaddyRoutesSettings> settings) => settings
        .Require(s => s.Release is { Length: > 0 }, "'release' not set in ritten.json: the directory the Caddyfile imports routes from.");

    protected override void Configure(IWorkflowBuilder builder, CaddyRoutesSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker();
        if (settings.Release is { Length: > 0 } release)
        {
            builder.AddReleases(release);
        }
    }
}
