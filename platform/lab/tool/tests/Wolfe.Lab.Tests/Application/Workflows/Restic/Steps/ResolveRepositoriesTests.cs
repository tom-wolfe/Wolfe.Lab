using Wolfe.Lab.Application.Workflows.Restic.Models;
using Wolfe.Lab.Application.Workflows.Restic.Steps;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Backups;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Restic;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Restic.Steps;

public class ResolveRepositoriesTests
{
    // The repositories as declared, then given what a test says they declare.
    private static DeploymentUnit Unit(Action<ResticComponent> declares)
    {
        var catalog = new ServiceCatalog();
        var service = Catalogs.AddService(catalog, "platform/restic").Value.ShouldNotBeNull();
        var repositories = ResticComponent.Create(new DocumentSource(RepositoryPath.From("platform/restic/repositories/component.yaml")),
            ComponentName.From("repositories"), ComponentKind.Repository).Value.ShouldNotBeNull();
        declares(repositories);
        service.Add(repositories).Value.ShouldNotBeNull();
        return catalog.DeploymentUnitAt(RepositoryPath.From("platform/restic/repositories")).ShouldNotBeNull().Value.ShouldNotBeNull();
    }

    private static readonly ResticOptions Former = new()
    {
        Retention = new RetentionOptions { Daily = 3, Weekly = 2, Monthly = 1 },
        Verify = new VerifyOptions { ReadDataSubset = "10%" }
    };

    private static ResticComponent Resolve(Action<ResticComponent> declares) =>
        new ResolveRepositories(Former, Substitute.For<IWorkflowLog>()).Run(Unit(declares)).Value.ShouldNotBeNull();

    [Fact]
    public void Run_TakesWhatTheRepositoriesDeclare()
    {
        var repositories = Resolve(declared =>
        {
            declared.Retention = RetentionPolicy.Create(SnapshotCount.From(7), SnapshotCount.From(5), SnapshotCount.From(12)).Value;
            declared.VerifySample = Percentage.From(5);
        });

        repositories.Retention.ShouldNotBeNull().Daily.ShouldBe(SnapshotCount.From(7));
        repositories.VerifySample.ShouldBe(Percentage.From(5));
    }

    [Fact]
    public void Run_TakesItsRittenJsonsUntilTheyDeclareIt()
    {
        var repositories = Resolve(_ => { });

        repositories.Retention.ShouldNotBeNull().Daily.ShouldBe(SnapshotCount.From(3));
        repositories.VerifySample.ShouldBe(Percentage.From(10));
    }
}
