using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Application.Catalog;
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
    private static readonly RunnerDefaults Defaults = new("Wolfe.Lab", "tom-wolfe/Wolfe.Lab", "node:22-bookworm");

    private static readonly ForgejoRunnerOptions Legacy = new() { Vault = "Wolfe.Lab", Repository = "tom-wolfe/Wolfe.Lab", Image = "node:22-bookworm" };

    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public ResolveRunnerTests()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory("/checkout"));
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory("/checkout/platform/forgejo/runners"));
    }

    private Task<StepResult<RunnerRegistration>> Resolve(RunnerRequest request, ServiceCatalog? catalog = null) =>
        new ResolveRunner(new DeclaredComponents(_git, _fileSystem), Legacy, request, Substitute.For<IWorkflowLog>()).Run(catalog ?? new ServiceCatalog(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Run_TakesTheDefaultsTheComponentDeclares()
    {
        var catalog = new ServiceCatalog();
        var service = Catalogs.AddService(catalog, "platform/forgejo").Value.ShouldNotBeNull();
        service.Add(ForgejoRunnerComponent.Create(new DocumentSource(RepositoryPath.From("platform/forgejo/runners/component.yaml")), ComponentName.From("runners"), ComponentKind.Runner,
            "Declared", "tom-wolfe/Declared", "node:24").Value.ShouldNotBeNull()).Value.ShouldNotBeNull();

        var registration = (await Resolve(new RunnerRequest("wolfe-pi5", RunnerKind.Host), catalog)).Value.ShouldNotBeNull();

        registration.Scope.ShouldBe("tom-wolfe/Declared");
        registration.Secret.Value.ShouldBe("op://Declared/forgejo-runner-wolfe-pi5/credential");
    }

    [Fact]
    public async Task Run_AHostRunnerIsNamedForItsNodeAndScopedToTheLab()
    {
        var registration = (await Resolve(new RunnerRequest("wolfe-pi5", RunnerKind.Host))).Value.ShouldNotBeNull();

        registration.Name.ShouldBe("wolfe-pi5");
        registration.Labels.ShouldBe("wolfe-pi5:host");
        registration.Scope.ShouldBe("tom-wolfe/Wolfe.Lab");
        registration.Secret.Value.ShouldBe("op://Wolfe.Lab/forgejo-runner-wolfe-pi5/credential");
    }

    [Fact]
    public async Task Run_AContainerisedRunnerCarriesTheRoleLabelInstanceWide()
    {
        var registration = (await Resolve(new RunnerRequest("wolfe-pi5", RunnerKind.Docker))).Value.ShouldNotBeNull();

        registration.Name.ShouldBe("wolfe-pi5-docker");
        registration.Labels.ShouldBe("docker:docker://node:22-bookworm");
        registration.Scope.ShouldBeNull();
        registration.Secret.Value.ShouldBe("op://Wolfe.Lab/forgejo-runner-wolfe-pi5-docker/credential");
    }

    [Fact]
    public void ToDefaults_IsNullWhileAnythingRequiredIsMissing()
    {
        new ForgejoRunnerOptions { Vault = "Wolfe.Lab", Repository = "tom-wolfe/Wolfe.Lab", Image = "node:22-bookworm" }.ToDefaults().ShouldBe(Defaults);
        new ForgejoRunnerOptions { Vault = "Wolfe.Lab" }.ToDefaults().ShouldBeNull();
    }
}
