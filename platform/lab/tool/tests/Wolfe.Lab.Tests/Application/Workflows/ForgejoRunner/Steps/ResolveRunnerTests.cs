using Wolfe.Lab.Application.Workflows.ForgejoRunner.Models;
using Wolfe.Lab.Application.Workflows.ForgejoRunner.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Forgejo;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.ForgejoRunner.Steps;

public class ResolveRunnerTests
{
    private static readonly DeploymentUnit Runners = Catalogs.UnitOf(ForgejoRunnerComponent.Create(new DocumentSource(RepositoryPath.From("platform/forgejo/runners/component.yaml")),
        ComponentName.From("runners"), ComponentKind.Runner, "Wolfe.Lab", "tom-wolfe/Wolfe.Lab", "node:22-bookworm"));

    private static StepResult<RunnerRegistration> Resolve(RunnerKind kind) =>
        new ResolveRunner(new RunnerRequest("wolfe-pi5", kind), Substitute.For<IWorkflowLog>()).Run(Runners);

    [Fact]
    public void Run_AHostRunnerIsNamedForItsNodeAndScopedToTheLab()
    {
        var registration = Resolve(RunnerKind.Host).Value.ShouldNotBeNull();

        registration.Name.ShouldBe("wolfe-pi5");
        registration.Labels.ShouldBe("wolfe-pi5:host");
        registration.Scope.ShouldBe("tom-wolfe/Wolfe.Lab");
        registration.Secret.Value.ShouldBe("op://Wolfe.Lab/forgejo-runner-wolfe-pi5/credential");
    }

    [Fact]
    public void Run_AContainerisedRunnerCarriesTheRoleLabelInstanceWide()
    {
        var registration = Resolve(RunnerKind.Docker).Value.ShouldNotBeNull();

        registration.Name.ShouldBe("wolfe-pi5-docker");
        registration.Labels.ShouldBe("docker:docker://node:22-bookworm");
        registration.Scope.ShouldBeNull();
        registration.Secret.Value.ShouldBe("op://Wolfe.Lab/forgejo-runner-wolfe-pi5-docker/credential");
    }
}
