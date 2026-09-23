using Wolfe.Lab.Build.Workflows.ForgejoRunners.Models;
using Wolfe.Lab.Build.Workflows.ForgejoRunners.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.ForgejoRunners.Steps;

public class ResolveRunnerTests
{
    private static readonly RunnerDefaults Defaults = new("Wolfe.Lab", "tom-wolfe/Wolfe.Lab", "node:22-bookworm");

    private static ResolveRunner Step(RunnerRequest request) => new(request, Defaults, Substitute.For<IWorkflowLog>());

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
    public void ToDefaults_IsNullWhileAnythingRequiredIsMissing()
    {
        new ForgejoRunnersSettings { Vault = "Wolfe.Lab", Repository = "tom-wolfe/Wolfe.Lab", Image = "node:22-bookworm" }.ToDefaults().ShouldBe(Defaults);
        new ForgejoRunnersSettings { Vault = "Wolfe.Lab" }.ToDefaults().ShouldBeNull();
    }
}
