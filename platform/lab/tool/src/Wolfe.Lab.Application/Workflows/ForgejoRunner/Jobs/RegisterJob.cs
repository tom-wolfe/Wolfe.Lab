using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Workflows.ForgejoRunner.Models;
using Wolfe.Lab.Application.Workflows.ForgejoRunner.Steps;

namespace Wolfe.Lab.Application.Workflows.ForgejoRunner.Jobs;

/// <summary>
/// Registers one node's runner with Forgejo.
/// </summary>
/// <remarks>
/// Registrations live in Forgejo's database, so a fresh Forgejo knows none of them while each
/// node's runner still holds its secret and polls with it. Same secret, same UUID: the node side
/// needs no change.
/// </remarks>
internal sealed class RegisterJob : LabJob<ForgejoRunnerOptions>
{
    private static readonly JobArgument<string> Node = JobArgument.Value<string>(
        "node",
        "The node whose runner to register, by its name: MacMini, MacStudio, wolfe-pi5.",
        required: true
    );

    private static readonly JobArgument<RunnerKind> RunnerKindArgument = JobArgument.Value(
        "kind",
        "Which runner: host (the default) or docker.",
        ParseKind
    );

    public override string Name => "register";

    public override string Description => "Registers a node's Actions runner with Forgejo, from the secret the vault holds for it.";

    public override IReadOnlyList<JobArgument> Arguments { get; } = [Node, RunnerKindArgument];

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveRunner>(),
        Step.FromType<GateApproval>(),
        Step.FromType<RegisterRunner>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    private static Result<RunnerKind> ParseKind(string text) =>
        Enum.TryParse<RunnerKind>(text, ignoreCase: true, out var kind) ? kind : new Error($"'{text}' is not a runner kind; host or docker.");

    public override bool RequiresProject => false;

    protected override void Configure(IWorkflowBuilder builder, ForgejoRunnerOptions options, JobArguments args)
    {
        base.Configure(builder, options, args);
        builder.AddDocker();
        builder.Services.AddSingleton(new RunnerRequest(args.Get(Node) ?? "", args.Get(RunnerKindArgument)));
        builder.Services.AddSingleton(options);
    }
}
