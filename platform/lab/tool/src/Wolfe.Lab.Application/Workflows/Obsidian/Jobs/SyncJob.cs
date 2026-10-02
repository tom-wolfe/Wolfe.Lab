using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Workflows.Obsidian.Models;
using Wolfe.Lab.Application.Workflows.Obsidian.Steps;
using Wolfe.Lab.Infrastructure.Obsidian;
using Wolfe.Lab.Infrastructure.Paths;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Jobs;

internal sealed class SyncJob : LabJob<ObsidianOptions>
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

    protected override void ValidateSettings(SettingsValidator<ObsidianOptions> options) => options
        .Require(s => s.Push.Username is not null, "'push.username' not set in ritten.json.")
        .Require(s => s.Push.Token is not null, "'push.token' not set in ritten.json.")
        .Require(s => s.Vaults.Count > 0, "'vaults' names no vault.")
        .Require(
            s => s.Vaults.All(v => v.Value.Path is not null && v.Value.Repository is not null),
            "every vault needs a 'path' and a 'repository'.");

    protected override void Configure(IWorkflowBuilder builder, ObsidianOptions options, JobArguments args)
    {
        base.Configure(builder, options);
        builder.AddObsidian();

        var vaults = options.Vaults
            .SelectMany(IEnumerable<KeyValuePair<string, Vault>> (v) => v.Value is { Path: { } path, Repository: { } repository }
                ? [KeyValuePair.Create(v.Key, new Vault(v.Key, path.Directory, repository))]
                : [])
            .ToDictionary();
        builder.Services.AddSingleton(new KnownVaults(vaults));
        if (args.Get(VaultArgument) is { } requested)
        {
            builder.Services.AddSingleton(new RequestedVault(requested));
        }

        if (options.Push is { Username: { } username, Token: { } token })
        {
            builder.Services.AddSingleton(new PushCredential(username, token));
        }
        builder.Services.AddSingleton(new VaultExcludes(options.Exclude));
    }
}
