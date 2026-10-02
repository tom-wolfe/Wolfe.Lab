using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Workflows.GatusHealth.Models;
using Wolfe.Lab.Application.Workflows.GatusHealth.Steps;
using Wolfe.Lab.Infrastructure.Gatus;

namespace Wolfe.Lab.Application.Workflows.GatusHealth.Jobs;

/// <summary>
/// Watches the watcher: fails, and so pages, when the status page is not up.
/// </summary>
internal sealed class ProbeJob : LabJob<GatusHealthOptions>
{
    public override string Name => "probe";

    public override string Description => "Reads Gatus's own health endpoint and fails unless it reports UP.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<ProbeHealth>()];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<GatusHealthOptions> options) => options
        .Require(s => s.Url is not null, "'url' not set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, GatusHealthOptions options)
    {
        base.Configure(builder, options);
        builder.AddGatus();
        if (options.ToProbe() is { } probe)
        {
            builder.Services.AddSingleton(probe);
        }
    }
}
