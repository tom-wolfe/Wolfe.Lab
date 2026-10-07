using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Tests.Domain.Catalog;

public class DeploymentUnitTests
{
    private const string Immich = "personal/immich/compose";

    private static Result<DeploymentUnit> At(string directory, params Catalogs.Declaration[] components) =>
        Catalogs.Of(directory, components).DeploymentUnitAt(RepositoryPath.From(directory)).ShouldNotBeNull();

    [Fact]
    public void Name_IsItsServicesAndItsHeads()
    {
        var unit = At(Immich,
            Catalogs.Compose("postgres", "immich-database", ComponentKind.Database),
            Catalogs.Compose("cache", "immich-redis", ComponentKind.Cache),
            Catalogs.Compose("server", "immich-server") with { DependsOn = ["postgres", "cache"] }).Value.ShouldNotBeNull();

        unit.Head.Name.Value.ShouldBe("server");
        unit.Name.ShouldBe("immich-server");
    }

    [Fact]
    public void Head_IsNeverAPartOfAnother()
    {
        var unit = At("platform/forgejo/compose",
            Catalogs.Compose("server", "server"),
            Catalogs.Compose("tailscale", "tailscale", ComponentKind.Network) with { PartOf = "server" }).Value.ShouldNotBeNull();

        unit.Name.ShouldBe("forgejo-server");
    }

    [Fact]
    public void Head_MayBePartOfWhatAnotherDirectoryDeclares()
    {
        var catalog = Catalogs.Of("media/sonarr/compose", Catalogs.Compose("server", "sonarr"));
        Catalogs.Add(catalog.Services.Single(), new DocumentSource(RepositoryPath.From("media/sonarr/backup/component.yaml")),
            Catalogs.Backup("config", "server", "/Users/lab/Docker/sonarr/config")).Value.ShouldNotBeNull();

        catalog.DeploymentUnitAt(RepositoryPath.From("media/sonarr/backup")).ShouldNotBeNull().Value.ShouldNotBeNull().Name.ShouldBe("sonarr-config");
    }

    [Fact]
    public void Create_RefusesSeveralAtTheHead() =>
        At(Immich, Catalogs.Compose("server", "immich-server"), Catalogs.Compose("machine-learning", "immich-machine-learning", ComponentKind.Model))
            .Errors.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<CatalogError>()
            .Problem.ShouldBe(DeploymentUnitErrors.NotOneHead(RepositoryPath.From(Immich), [ComponentName.From("machine-learning"), ComponentName.From("server")]));

    [Fact]
    public void RequiresVolumes_IsEveryComponentsOnce()
    {
        var catalog = Catalogs.Of(Immich,
            Catalogs.Compose("machine-learning", "immich-machine-learning", ComponentKind.Model),
            Catalogs.Compose("server", "immich-server") with { DependsOn = ["machine-learning"] });
        foreach (var component in catalog.Services.Single().Components)
        {
            component.RequiresVolumes = [HostPath.From("/Volumes/Data2")];
        }

        catalog.DeploymentUnitAt(RepositoryPath.From(Immich)).ShouldNotBeNull().Value.ShouldNotBeNull()
            .RequiresVolumes.ShouldBe([HostPath.From("/Volumes/Data2")]);
    }
}
