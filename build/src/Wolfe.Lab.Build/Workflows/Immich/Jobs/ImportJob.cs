using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Slices.Steps;
using Wolfe.Lab.Build.Workflows.Common.Steps;
using Wolfe.Lab.Build.Workflows.Immich.Models;
using Wolfe.Lab.Build.Workflows.Immich.Steps;

namespace Wolfe.Lab.Build.Workflows.Immich.Jobs;

/// <summary>
/// Imports the Google Takeout into the running server.
/// </summary>
internal sealed class ImportJob : LabJob<ImmichSettings>
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
        builder.AddDocker();
        builder.Services.AddSingleton(new ImportOptions(settings.Import.Concurrency));

        // Validation has already refused a missing value; the patterns keep that promise in the
        // types rather than restating it with a null-forgiving operator.
        if (settings.Takeout.Path is { } takeout)
        {
            builder.Services.AddSingleton(new TakeoutLocation(takeout.Directory));
        }

        if (settings.Server is { Url: { } url, ApiKey: { } apiKey })
        {
            builder.Services.AddSingleton(new ImmichServer(url, apiKey));
        }
    }
}
