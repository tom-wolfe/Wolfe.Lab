using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Workflows.Obsidian.Models;
using Wolfe.Lab.Application.Workflows.Obsidian.Steps;
using Wolfe.Lab.Infrastructure.Obsidian;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Jobs;

internal sealed class SyncJob : LabJob<ObsidianOptions>
{
    private static readonly JobArgument<string> VaultArgument =
        JobArgument.Value<string>("vault", "The vault to sync, as ritten.json names it; none when the directory declares its vault.");

    public override string Name => "sync";

    public override string Description =>
        "Pulls the vault from Obsidian Sync, commits what changed and pushes it to Forgejo.";

    public override JobKind Kind => JobKind.Work;

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveVault>(),
        Step.FromType<SyncVault>(),
        Step.FromType<CommitVault>(),
        Step.FromType<GateApproval>(),
        Step.FromType<PushVault>()
    ];

    public override IReadOnlyList<JobArgument> Arguments { get; } = [VaultArgument];

    // Its vault may be declared rather than in ritten.json (ROADMAP #14, step 7).
    public override bool RequiresProject => false;

    protected override void Configure(IWorkflowBuilder builder, ObsidianOptions options, JobArguments args)
    {
        base.Configure(builder, options);
        builder.AddObsidian();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(new RequestedVault(args.Get(VaultArgument) ?? ""));
    }
}
