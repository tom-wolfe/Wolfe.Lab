using Microsoft.Extensions.Options;
using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Backup.Models;
using Wolfe.Lab.Application.Workflows.Backup.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Backup.Steps;

public class ResolveBackupPlanTests
{
    private const string Backup = "media/sonarr/backup";
    private readonly IDocker _docker = Substitute.For<IDocker>();

    public ResolveBackupPlanTests() =>
        _docker.ComposeConfig(Arg.Is<IDirectory>(directory => directory.AbsolutePath == "/lab/root/sonarr-server"), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ComposeProject([new ComposeService("sonarr", new Dictionary<string, string>(), new Dictionary<string, string?>(), []) { ContainerName = "sonarr-app" }]));

    // Sonarr's stack, and its backup declared beside it.
    private static ServiceCatalog Catalog(Catalogs.Declaration backup)
    {
        var catalog = Catalogs.Of("media/sonarr/compose", Catalogs.Compose("server", "sonarr"));
        Catalogs.Add(catalog.Services.Single(), new DocumentSource(RepositoryPath.From($"{Backup}/component.yaml")), backup).Value.ShouldNotBeNull();
        return catalog;
    }

    private Task<StepResult<BackupPlan>> Resolve(ServiceCatalog catalog) =>
        new ResolveBackupPlan(_docker, Options.Create(new LabDirectories { Root = new PhysicalDirectory("/lab/root") }), Substitute.For<IWorkflowLog>())
            .Run(catalog, catalog.DeploymentUnitAt(RepositoryPath.From(Backup)).ShouldNotBeNull().Value.ShouldNotBeNull(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Run_StopsTheStackItIsPartOf_AndTagsWithItsContainersImage()
    {
        var plan = (await Resolve(Catalog(Catalogs.Backup("config", "server", "/Users/lab/Docker/sonarr/config")))).Value.ShouldNotBeNull();

        plan.Tag.ShouldBe("service:sonarr");
        plan.Paths.ShouldHaveSingleItem().AbsolutePath.ShouldBe("/Users/lab/Docker/sonarr/config");
        plan.Stack.ShouldNotBeNull().AbsolutePath.ShouldBe("/lab/root/sonarr-server");
        plan.Container.ShouldBe("sonarr-app");
        plan.Image.ShouldBe("sonarr-app");
    }

    [Fact]
    public async Task Run_StopsNothingForAWarmBackup_ButStillTagsIt()
    {
        var warm = Catalogs.Backup("config", "server") with { Backup = new Catalogs.Snapshot(["/Users/lab/Docker/sonarr/config"], Warm: true) };

        var plan = (await Resolve(Catalog(warm))).Value.ShouldNotBeNull();

        plan.Stack.ShouldBeNull();
        plan.Container.ShouldBeNull();
        plan.Image.ShouldBe("sonarr-app");
    }

    [Fact]
    public async Task Run_AsksDockerNothingForABackupPartOfNothing()
    {
        var plan = (await Resolve(Catalog(Catalogs.Backup("files", null, "/Volumes/Data2/files")))).Value.ShouldNotBeNull();

        (plan.Stack, plan.Container, plan.Image).ShouldBe((null, null, null));
        _docker.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_RefusesAStackThatRunsNoServiceForWhatItIsPartOf()
    {
        _docker.ComposeConfig(Arg.Any<IDirectory>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ComposeProject([]));

        (await Resolve(Catalog(Catalogs.Backup("config", "server", "/Users/lab/Docker/sonarr/config"))))
            .Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("runs no sonarr service");
    }
}
