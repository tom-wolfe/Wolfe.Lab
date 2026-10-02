using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;

namespace Wolfe.Lab.Application.Workflows.Ollama.Jobs;

/// <summary>
/// Proves the component's roles are sound before any node points a name at a model.
/// </summary>
internal sealed class CheckJob : LabJob<OllamaOptions>
{
    public override string Name => "check";

    public override string Description => "Checks the component's model roles, alone and against the service's other servers.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<GatePathFilter>(), Step.FromType<CheckRoles>()];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, OllamaOptions options)
    {
        base.Configure(builder, options);
        builder.Services.AddSingleton(new DeclaredRoles(options.Models));
    }
}
