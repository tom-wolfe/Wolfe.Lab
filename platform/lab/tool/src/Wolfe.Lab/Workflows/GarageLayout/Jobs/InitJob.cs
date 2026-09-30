using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Clients.Garage;
using Wolfe.Lab.Clients.Gates.Steps;
using Wolfe.Lab.Workflows.GarageLayout.Models;
using Wolfe.Lab.Workflows.GarageLayout.Steps;

namespace Wolfe.Lab.Workflows.GarageLayout.Jobs;

/// <summary>
/// The one storage setup that must happen before the admin API is usable. Buckets, keys and
/// grants are tofu's; this is the cluster telling itself it has a node.
/// </summary>
internal sealed class InitJob : LabJob<GarageLayoutOptions>
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

    protected override void ValidateSettings(SettingsValidator<GarageLayoutOptions> options) => options
        .Require(s => s.ToLayout() is not null, "'zone' and 'capacity' must both be set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, GarageLayoutOptions options)
    {
        base.Configure(builder, options);
        builder.AddGarage();
        if (options.ToLayout() is { } layout)
        {
            builder.Services.AddSingleton(layout);
        }
    }
}
