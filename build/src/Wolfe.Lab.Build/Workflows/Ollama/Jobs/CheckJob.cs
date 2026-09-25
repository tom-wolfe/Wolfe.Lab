using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.Ollama.Models;
using Wolfe.Lab.Build.Workflows.Ollama.Steps;

namespace Wolfe.Lab.Build.Workflows.Ollama.Jobs;

/// <summary>
/// Proves the component's roles are sound before any node points a name at a model.
/// </summary>
internal sealed class CheckJob : LabJob<OllamaSettings>
{
    public override string Name => "check";

    public override string Description => "Checks the component's model roles, alone and against the slice's other servers.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<GatePathFilter>(), Step.FromType<CheckRoles>()];

    public override JobKind Kind => JobKind.Check;

    protected override void Configure(IWorkflowBuilder builder, OllamaSettings settings)
    {
        base.Configure(builder, settings);
        builder.Services.AddSingleton(new DeclaredRoles(settings.Models));
    }
}
