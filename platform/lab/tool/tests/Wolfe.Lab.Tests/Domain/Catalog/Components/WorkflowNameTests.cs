using Ritten.Engine.Workflows;
using Wolfe.Lab.Application.Workflows.Docker;
using Wolfe.Lab.Domain.Catalog.Components;

namespace Wolfe.Lab.Tests.Domain.Catalog.Components;

public class WorkflowNameTests
{
    // Every workflow the CLI has, by the name a ritten.json gives it.
    private static IReadOnlyList<string> Workflows() =>
    [
        .. typeof(DockerWorkflow).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true } && typeof(IWorkflow).IsAssignableFrom(type))
            .Select(type => (Activator.CreateInstance(type) as IWorkflow)?.Name ?? throw new InvalidOperationException($"{type.Name} is not a workflow."))
    ];

    // A declaration names the workflow that operates it, so each it may name must be one that runs.
    [Fact]
    public void All_IsWorkflowsTheCliHas() =>
        WorkflowName.All.Select(workflow => workflow.Value).ShouldBeSubsetOf(Workflows());
}
