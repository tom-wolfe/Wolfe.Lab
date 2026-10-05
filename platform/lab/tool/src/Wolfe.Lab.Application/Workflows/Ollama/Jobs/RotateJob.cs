using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;
using Wolfe.Lab.Infrastructure.Logrotate;

namespace Wolfe.Lab.Application.Workflows.Ollama.Jobs;

/// <summary>
/// Rotates the model server's logs on this node.
/// </summary>
internal sealed class RotateJob : LabJob<OllamaOptions>
{
    public override string Name => "rotate";

    public override string Description => "Rotates the logs the model server writes on this node.";

    public override JobKind Kind => JobKind.Work;

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveOllamaAgents>(),
        Step.FromType<GateApproval>(),
        Step.FromType<RotateAgentLogs>()
    ];

    protected override void ValidateSettings(SettingsValidator<OllamaOptions> options) => options
        .Require(s => s.Agents.Count > 0, "'agents' names nothing in ritten.json.");

    protected override void Configure(IWorkflowBuilder builder, OllamaOptions options)
    {
        base.Configure(builder, options);
        builder.AddLogrotate();
        builder.Services.AddSingleton(new OllamaAgents(options.Agents));
    }
}
