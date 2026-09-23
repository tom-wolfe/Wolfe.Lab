using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Gatus;
using Wolfe.Lab.Build.Workflows.GatusHealth.Models;
using Wolfe.Lab.Build.Workflows.GatusHealth.Steps;

namespace Wolfe.Lab.Build.Workflows.GatusHealth.Jobs;

/// <summary>
/// Watches the watcher: fails, and so pages, when the status page is not up.
/// </summary>
internal sealed class ProbeJob : LabJob<GatusHealthSettings>
{
    public override string Name => "probe";

    public override string Description => "Reads Gatus's own health endpoint and fails unless it reports UP.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<ProbeHealth>()];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<GatusHealthSettings> settings) => settings
        .Require(s => s.Url is not null, "'url' not set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, GatusHealthSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddGatus();
        if (settings.ToProbe() is { } probe)
        {
            builder.Services.AddSingleton(probe);
        }
    }
}
