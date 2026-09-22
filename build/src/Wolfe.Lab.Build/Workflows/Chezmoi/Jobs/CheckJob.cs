using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Chezmoi;
using Wolfe.Lab.Build.Workflows.Chezmoi.Models;
using Wolfe.Lab.Build.Workflows.Chezmoi.Steps;
using Wolfe.Lab.Build.Workflows.Common.Steps;

namespace Wolfe.Lab.Build.Workflows.Chezmoi.Jobs;

/// <summary>
/// Proves the source renders for every machine and that what it renders is valid shell.
/// </summary>
internal sealed class CheckJob : LabJob<ChezmoiSettings>
{
    public override string Name => "check";

    public override string Description => "Renders the source for every profile, vault stubbed, and shellchecks the rendered scripts.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<RenderProfiles>(),
        Step.FromType<CheckScripts>()
    ];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<ChezmoiSettings> settings) => settings
        .Require(s => s.Profiles.Count > 0, "'profiles' names nothing in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, ChezmoiSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddChezmoi();
        builder.Services.AddSingleton(new Profiles(settings.Profiles));
    }
}
