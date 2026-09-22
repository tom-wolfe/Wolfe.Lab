using Wolfe.Lab.Build.Workflows.Forgejo.Models;
using Wolfe.Lab.Build.Workflows.Forgejo.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Forgejo.Steps;

public class ResolveRunnerTests
{
    private static readonly RunnerSettings Settings = new() { Vault = "Wolfe.Lab", Repository = "tom-wolfe/Wolfe.Lab", Image = "node:22-bookworm" };

    private static ResolveRunner Step(RunnerRequest request, RunnerSettings? settings = null) => new(request, settings ?? Settings, Substitute.For<IWorkflowLog>());

    [Fact]
    public void Run_AHostRunnerIsNamedForItsNodeAndScopedToTheLab()
    {
        var registration = Step(new RunnerRequest("wolfe-pi5", RunnerKind.Host)).Run().Value.ShouldNotBeNull();

        registration.Name.ShouldBe("wolfe-pi5");
        registration.Labels.ShouldBe("wolfe-pi5:host");
        registration.Scope.ShouldBe("tom-wolfe/Wolfe.Lab");
        registration.Secret.Value.ShouldBe("op://Wolfe.Lab/forgejo-runner-wolfe-pi5/credential");
    }

    [Fact]
    public void Run_AContainerisedRunnerCarriesTheRoleLabelInstanceWide()
    {
        var registration = Step(new RunnerRequest("wolfe-pi5", RunnerKind.Docker)).Run().Value.ShouldNotBeNull();

        registration.Name.ShouldBe("wolfe-pi5-docker");
        registration.Labels.ShouldBe("docker:docker://node:22-bookworm");
        registration.Scope.ShouldBeNull();
        registration.Secret.Value.ShouldBe("op://Wolfe.Lab/forgejo-runner-wolfe-pi5-docker/credential");
    }

    [Fact]
    public void Run_RefusesAnIncompleteSection()
    {
        Step(new RunnerRequest("MacMini", RunnerKind.Host), new RunnerSettings { Vault = "Wolfe.Lab" }).Run().Outcome.IsFailure.ShouldBeTrue();
    }
}
