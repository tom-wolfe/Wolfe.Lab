using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.ForgejoRunners.Models;
using Wolfe.Lab.Build.Workflows.ForgejoRunners.Steps;

namespace Wolfe.Lab.Build.Workflows.ForgejoRunners.Jobs;

/// <summary>
/// Registers one node's runner with Forgejo.
/// </summary>
/// <remarks>
/// Registrations live in Forgejo's database, so a fresh Forgejo knows none of them while each
/// node's runner still holds its secret and polls with it. Same secret, same UUID: the node side
/// needs no change.
/// </remarks>
internal sealed class RegisterJob : LabJob<ForgejoRunnersSettings>
{
    internal static readonly JobArgument<string> Node = JobArgument.Value<string>(
        "node",
        "The node whose runner to register, by its name: MacMini, MacStudio, wolfe-pi5.",
        required: true);

    internal static readonly JobArgument<RunnerKind> RunnerKindArgument = JobArgument.Value<RunnerKind>(
        "kind",
        "Which runner: host (the default) or docker.",
        ParseKind);

    public override string Name => "register";

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

    protected override void ValidateSettings(SettingsValidator<ForgejoRunnersSettings> settings) => settings
        .Require(s => s.ToDefaults() is not null, "'vault', 'repository' and 'image' must all be set in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, ForgejoRunnersSettings settings, JobArguments args)
    {
        base.Configure(builder, settings, args);
        builder.Services.AddSingleton(new RunnerRequest(args.Get(Node) ?? "", args.Get(RunnerKindArgument)));
        if (settings.ToDefaults() is { } defaults)
        {
            builder.Services.AddSingleton(defaults);
        }
    }
}
