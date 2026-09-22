using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Workflows.Common.Steps;
using Wolfe.Lab.Build.Workflows.Forgejo.Models;
using Wolfe.Lab.Build.Workflows.Forgejo.Steps;

namespace Wolfe.Lab.Build.Workflows.Forgejo.Jobs;

/// <summary>
/// Registers a node's Actions runner with the running Forgejo. Run on the mini, where the
/// container is, after the node's vault item exists.
/// </summary>
internal sealed class RegisterRunnerJob : LabJob<ForgejoSettings>
{
    internal static readonly JobArgument<string> Node = JobArgument.Value<string>(
        "node",
        "The node whose runner to register, by its name: MacMini, wolfe-pi5.",
        required: true);

    internal static readonly JobArgument<RunnerKind> RunnerKindArgument = JobArgument.Value<RunnerKind>(
        "kind",
        "Which runner: host (the default) or docker.",
        ParseKind);

    public override string Name => "register-runner";

    public override string Description => "Registers a node's Actions runner with Forgejo, from the secret the vault holds for it.";

    public override IReadOnlyList<JobArgument> Arguments { get; } = [Node, RunnerKindArgument];

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveRunner>(),
        Step.FromType<GateApproval>(),
        Step.FromType<RegisterRunner>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    private static Result<RunnerKind> ParseKind(string text) =>
        Enum.TryParse<RunnerKind>(text, ignoreCase: true, out var kind) ? kind : new Error($"'{text}' is not a runner kind; host or docker.");

    protected override void Configure(IWorkflowBuilder builder, ForgejoSettings settings, JobArguments args)
    {
        base.Configure(builder, settings, args);
        builder.Services.AddSingleton(settings.Runners);
        builder.Services.AddSingleton(new RunnerRequest(args.Get(Node) ?? "", args.Get(RunnerKindArgument)));
    }
}
