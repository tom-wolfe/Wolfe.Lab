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
    private static readonly RetentionPolicy Policy = RetentionPolicy.Create(SnapshotCount.From(7), SnapshotCount.From(5), SnapshotCount.From(12)).Value.ShouldNotBeNull();

    // The repositories, as declared.
    private static DeploymentUnit Unit(Percentage? sample = null)
    {
        var catalog = new ServiceCatalog();
        var service = Catalogs.AddService(catalog, "platform/restic").Value.ShouldNotBeNull();
        var repositories = ResticComponent.Create(new DocumentSource(RepositoryPath.From("platform/restic/repositories/component.yaml")),
            ComponentName.From("repositories"), ComponentKind.Repository, Policy).Value.ShouldNotBeNull();
        if (sample is { } declared)
        {
            repositories.VerifySample = declared;
        }

        service.Add(repositories).Value.ShouldNotBeNull();
        return catalog.DeploymentUnitAt(RepositoryPath.From("platform/restic/repositories")).ShouldNotBeNull().Value.ShouldNotBeNull();
    }

    private static ResticComponent Resolve(DeploymentUnit unit) =>
        new ResolveRepositories(Substitute.For<IWorkflowLog>()).Run(unit).Value.ShouldNotBeNull();

    [Fact]
    public void Run_TakesWhatTheRepositoriesDeclare()
    {
        var repositories = Resolve(Unit(Percentage.From(10)));

        repositories.Retention.ShouldBe(Policy);
        repositories.VerifySample.ShouldBe(Percentage.From(10));
    }

    [Fact]
    public void Run_ReadsFivePercentBackWhenTheyDeclareNoSample() =>
        Resolve(Unit()).VerifySample.ShouldBe(Percentage.From(5));
}
