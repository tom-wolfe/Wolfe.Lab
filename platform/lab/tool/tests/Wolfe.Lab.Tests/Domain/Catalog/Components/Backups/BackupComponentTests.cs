using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Backups;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Tests.Domain.Catalog.Components.Backups;

public class BackupComponentTests
{
    private static readonly DocumentSource Source = new(RepositoryPath.From("media/sonarr/backup/component.yaml"));

    private static Result<BackupComponent> Create(params string[] paths) =>
        BackupComponent.Create(Source, ComponentName.From("config"), ComponentKind.Storage, [.. paths.Select(HostPath.From)]);

    // A backup of paths, given what else the test says, added beside Sonarr's server.
    private static Result<Component> Add(string[] paths, string[]? excludes = null, string[]? verify = null, bool warm = false, string? partOf = "server") =>
        Catalogs.Add(Catalogs.Of("media/sonarr/compose", Catalogs.Docker("server", "sonarr")).Services.Single(), Source,
            Catalogs.Backup("config", partOf, paths) with { Backup = new Catalogs.Snapshot(paths, excludes, verify, warm) });

    private static IReadOnlyList<(string? Field, Error Problem)> Problems<T>(Result<T> result) where T : class =>
        [.. result.Errors.ShouldNotBeNull().Cast<CatalogError>().Select(error => (error.Field, error.Problem))];

    [Fact]
    public void Create_IsOperatedByTheBackupWorkflow() =>
        Create("/Users/lab/Docker/sonarr/config").Value.ShouldNotBeNull().Workflow.ShouldBe(WorkflowName.Backup);

    [Fact]
    public void Create_RefusesNothingToSnapshot() =>
        Problems(Create()).ShouldBe([("paths", BackupErrors.NothingToSnapshot)]);

    [Fact]
    public void Add_RefusesWhatIsLeftOutOrVerifiedOutsideThePaths() =>
        Problems(Add(["/Users/lab/Docker/sonarr/config"], excludes: ["/Users/lab/Docker/sonarr/config/logs", "/Users/lab/Docker/sonarr/configs"],
                verify: ["/Users/lab/Docker/radarr/config/radarr.db"]))
            .ShouldBe([
                ("excludes.1", BackupErrors.OutsideThePaths(HostPath.From("/Users/lab/Docker/sonarr/configs"))),
                ("verify.0", BackupErrors.OutsideThePaths(HostPath.From("/Users/lab/Docker/radarr/config/radarr.db")))
            ]);

    [Fact]
    public void Add_RefusesAWarmBackupOfNothing() =>
        Problems(Add(["/Volumes/Data2/files"], warm: true, partOf: null)).ShouldBe([("warm", BackupErrors.WarmOfNothing)]);

    [Fact]
    public void Stops_WhatItIsPartOf_UnlessWarm()
    {
        var catalog = Catalogs.Of("media/sonarr/compose", Catalogs.Docker("server", "sonarr"));
        var service = catalog.Services.Single();
        var cold = Catalogs.Add(service, new DocumentSource(RepositoryPath.From("media/sonarr/backup/component.yaml"), 0, 2),
            Catalogs.Backup("config", "server", "/Users/lab/Docker/sonarr/config")).Value.ShouldBeOfType<BackupComponent>();
        var warm = Catalogs.Add(service, new DocumentSource(RepositoryPath.From("media/sonarr/backup/component.yaml"), 1, 2),
            Catalogs.Backup("media", "server", "/Volumes/Data2/sonarr") with { Backup = new Catalogs.Snapshot(["/Volumes/Data2/sonarr"], Warm: true) })
            .Value.ShouldBeOfType<BackupComponent>();

        cold.Stops.ShouldBeSameAs(service.FindComponent(ComponentName.From("server")));
        warm.Stops.ShouldBeNull();
        warm.Host.ShouldBeSameAs(service.FindComponent(ComponentName.From("server")));
    }
}
