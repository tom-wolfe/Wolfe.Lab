using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolfe.Lab.Build.Clients.Garage;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.GarageLayout.Models;
using Wolfe.Lab.Build.Workflows.GarageLayout.Steps;

namespace Wolfe.Lab.Build.Workflows.GarageLayout.Jobs;

/// <summary>
/// The one storage setup that must happen before the admin API is usable. Buckets, keys and
/// grants are tofu's; this is the cluster telling itself it has a node.
/// </summary>
internal sealed class InitJob : LabJob<GarageLayoutSettings>
{
    public override string Name => "init";

    public override string Description => "Applies Garage's first cluster layout; a no-op once one exists.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<WaitForGarage>(),
        Step.FromType<GateApproval>(),
        Step.FromType<InitLayout>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<GarageLayoutSettings> settings) => settings
        .Require(s => s.ToLayout() is not null, "'zone' and 'capacity' must both be set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, GarageLayoutSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddGarage();
        builder.Services.TryAddSingleton(TimeProvider.System);
        if (settings.ToLayout() is { } layout)
        {
            builder.Services.AddSingleton(layout);
        }
    }
}
