using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Clients.Obsidian;
using Wolfe.Lab.Build.Workflows.Obsidian.Models;
using Wolfe.Lab.Build.Workflows.Obsidian.Steps;

namespace Wolfe.Lab.Build.Workflows.Obsidian.Jobs;

internal sealed class SyncJob : LabJob<ObsidianSettings>
{
    private static readonly JobArgument<string> VaultArgument =
        JobArgument.Value<string>("vault", "The vault to sync, as ritten.json names it.", required: true);

    public override string Name => "sync";

    public override string Description =>
        "Pulls the vault from Obsidian Sync, commits what changed and pushes it to Forgejo.";

    public override JobKind Kind => JobKind.Work;

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveVault>(),
        Step.FromType<SyncVault>(),
        Step.FromType<CommitVault>(),
        Step.FromType<GateApproval>(),
        Step.FromType<PushVault>()
    ];

    public override IReadOnlyList<JobArgument> Arguments { get; } = [VaultArgument];

    protected override void ValidateSettings(SettingsValidator<ObsidianSettings> settings) => settings
        .Require(s => s.Push.Username is not null, "'push.username' not set in ritten.json.")
        .Require(s => s.Push.Token is not null, "'push.token' not set in ritten.json.")
        .Require(s => s.Vaults.Count > 0, "'vaults' names no vault.")
        .Require(
            s => s.Vaults.All(v => v.Value.Path is not null && v.Value.Repository is not null),
            "every vault needs a 'path' and a 'repository'.");

    protected override void Configure(IWorkflowBuilder builder, ObsidianSettings settings, JobArguments args)
    {
        base.Configure(builder, settings);
        builder.AddObsidian();

        var vaults = settings.Vaults.ToDictionary(
            v => v.Key,
            v => new Vault(v.Key, v.Value.Path!.Value.Directory, v.Value.Repository!.Value));
        builder.Services.AddSingleton(new KnownVaults(vaults));
        builder.Services.AddSingleton(new RequestedVault(args.Get(VaultArgument)!));
        builder.Services.AddSingleton(new PushCredential(settings.Push.Username!.Value, settings.Push.Token!.Value));
        builder.Services.AddSingleton(new VaultExcludes(settings.Exclude));
    }
}
