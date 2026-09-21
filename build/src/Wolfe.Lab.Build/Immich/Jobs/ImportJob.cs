using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Immich.Models;
using Wolfe.Lab.Build.Immich.Steps;
using Wolfe.Lab.Build.Steps;

namespace Wolfe.Lab.Build.Immich.Jobs;

/// <summary>
/// Imports the Google Takeout into the running server.
/// </summary>
internal sealed class ImportJob : ImmichJob
{
    public override string Name => "import";

    public override string Description => "Imports the Google Takeout into Immich with immich-go, metadata and albums included.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveSlice>(),
        Step.FromType<ResolveTakeout>(),
        Step.FromType<BuildImmichGo>(),
        Step.FromType<GateApproval>(),
        Step.FromType<ImportTakeout>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<ImmichSettings> settings) => settings
        .Require(s => s.Server.Url is not null, "'server.url' not set in ritten.json.")
        .Require(s => s.Server.ApiKey is not null, "'server.apiKey' not set in ritten.json.")
        .Require(s => s.Takeout.Path is not null, "'takeout.path' not set in ritten.json.")
        .Require(s => s.Import.Concurrency is >= 1 and <= 20, "'import.concurrency' must be between 1 and 20.");

    protected override void Configure(IWorkflowBuilder builder, ImmichSettings settings)
    {
        base.Configure(builder, settings);
        builder.Services.AddSingleton(new TakeoutLocation(settings.Takeout.Path!.Value.Directory));
        builder.Services.AddSingleton(new ImmichServer(settings.Server.Url!.Value, settings.Server.ApiKey!.Value));
        builder.Services.AddSingleton(new ImportOptions(settings.Import.Concurrency));
    }
}
