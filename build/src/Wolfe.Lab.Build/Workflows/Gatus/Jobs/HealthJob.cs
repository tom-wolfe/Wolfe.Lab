using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Gatus;
using Wolfe.Lab.Build.Workflows.Gatus.Models;
using Wolfe.Lab.Build.Workflows.Gatus.Steps;

namespace Wolfe.Lab.Build.Workflows.Gatus.Jobs;

/// <summary>
/// Watches the watcher: fails, and so pages, when the status page is not up.
/// </summary>
internal sealed class HealthJob : LabJob<GatusSettings>
{
    public override string Name => "health";

    public override string Description => "Reads Gatus's own health endpoint and fails unless it reports UP.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<ProbeHealth>()];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<GatusSettings> settings) => settings
        .Require(s => s.Health.Url is not null, "'health.url' not set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, GatusSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddGatus();
        if (settings.Health.ToProbe() is { } probe)
        {
            builder.Services.AddSingleton(probe);
        }
    }
}
