using Wolfe.Lab.Application.Workflows.Chezmoi.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Chezmoi;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Chezmoi.Steps;

public class ResolveProfilesTests
{
    [Fact]
    public void Run_IsTheProfilesTheComponentDeclares()
    {
        var unit = Catalogs.UnitOf(ChezmoiComponent.Create(new DocumentSource(RepositoryPath.From("platform/chezmoi/profiles/component.yaml")), ComponentName.From("profiles"),
            ComponentKind.Machine, [ChezmoiProfile.From("macbook"), ChezmoiProfile.From("pi-node")]));

        new ResolveProfiles(Substitute.For<IWorkflowLog>()).Run(unit).Value.ShouldNotBeNull()
            .Profiles.ShouldBe([ChezmoiProfile.From("macbook"), ChezmoiProfile.From("pi-node")]);
    }

    [Fact]
    public void Run_FailsInADirectoryChezmoiDoesNotOperate() =>
        new ResolveProfiles(Substitute.For<IWorkflowLog>()).Run(Catalogs.Unit("platform/chezmoi/compose", Catalogs.Docker("server", "server")))
            .Outcome.IsFailure.ShouldBeTrue();
}
