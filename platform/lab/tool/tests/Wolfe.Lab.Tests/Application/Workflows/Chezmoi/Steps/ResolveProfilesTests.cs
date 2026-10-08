using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.Chezmoi.Models;
using Wolfe.Lab.Application.Workflows.Chezmoi.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Chezmoi;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Chezmoi.Steps;

public class ResolveProfilesTests
{
    private const string Checkout = "/checkout";

    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public ResolveProfilesTests()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory(Checkout));
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory($"{Checkout}/platform/chezmoi/profiles"));
    }

    [Fact]
    public async Task TakesTheProfilesTheComponentDeclares()
    {
        var catalog = new ServiceCatalog();
        var service = Catalogs.AddService(catalog, "platform/chezmoi").Value.ShouldNotBeNull();
        var source = new DocumentSource(RepositoryPath.From("platform/chezmoi/profiles/component.yaml"));
        service.Add(ChezmoiComponent.Create(source, ComponentName.From("profiles"), ComponentKind.Machine, [ChezmoiProfile.From("macbook")]).Value.ShouldNotBeNull()).Value.ShouldNotBeNull();

        var profiles = await Resolve(catalog, new ChezmoiOptions { Profiles = ["pi-node"] });

        profiles.Value.ShouldNotBeNull().Names.ShouldBe([ChezmoiProfile.From("macbook")]);
    }

    [Fact]
    public async Task FallsBackToRittenJsonWhileNothingIsDeclared()
    {
        var profiles = await Resolve(new ServiceCatalog(), new ChezmoiOptions { Profiles = ["pi-node"] });

        profiles.Value.ShouldNotBeNull().Names.ShouldBe([ChezmoiProfile.From("pi-node")]);
    }

    [Fact]
    public async Task FailsWithNoProfiles() =>
        (await Resolve(new ServiceCatalog(), new ChezmoiOptions())).Outcome.IsFailure.ShouldBeTrue();

    private Task<StepResult<Profiles>> Resolve(ServiceCatalog catalog, ChezmoiOptions legacy) =>
        new ResolveProfiles(new DeclaredComponents(_git, _fileSystem), legacy, Substitute.For<IWorkflowLog>()).Run(catalog, TestContext.Current.CancellationToken);
}
