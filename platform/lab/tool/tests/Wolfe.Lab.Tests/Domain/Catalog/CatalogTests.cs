using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Tests.Domain.Catalog;

public class CatalogTests
{
    private static Declared<Service> Service(string file, string name, params string[] dependsOn) =>
        new(new Service(ServiceName.From(name), null, "A service.", Lifecycle.Production, [], [.. dependsOn.Select(ServiceName.From)]), new DocumentSource(RepositoryPath.From(file)));

    private static Declared<Component> Component(string file, string? name = null, string[]? dependsOn = null, int index = 0, int documents = 1) =>
        new(new Component(name is null ? null : ComponentName.From(name), null, null, ComponentType.Compose, [.. (dependsOn ?? []).Select(ComponentName.From)]),
            new DocumentSource(RepositoryPath.From(file), index, documents));

    private static IReadOnlyList<string> Errors(Result<Wolfe.Lab.Domain.Catalog.Catalog> result) =>
        [.. result.Errors.ShouldNotBeNull().Select(error => error.Message)];

    [Fact]
    public void From_PlacesComponentsInTheServiceWhoseDirectoryHoldsThem()
    {
        var catalog = Wolfe.Lab.Domain.Catalog.Catalog.Read(
            [Service("personal/immich/service.yaml", "immich")],
            [
                Component("personal/immich/compose/component.yaml"),
                Component("personal/immich/vaults.yaml", "main-vault", index: 0, documents: 2),
                Component("personal/immich/vaults.yaml", "dnd-vault", ["main-vault"], index: 1, documents: 2)
            ]).Value.ShouldNotBeNull();

        var service = catalog.Services.ShouldHaveSingleItem();
        service.Area.Value.ShouldBe("personal");
        service.Components.Select(component => component.Name.Value).ShouldBe(["compose", "dnd-vault", "main-vault"]);
        catalog.ComponentAt(RepositoryPath.From("personal/immich/compose")).ShouldNotBeNull().Source.ToString().ShouldBe("personal/immich/compose/component.yaml");
        service.Components.Single(component => component.Name.Value == "dnd-vault").Source.ToString().ShouldBe("personal/immich/vaults.yaml#2");
    }

    [Theory]
    [InlineData("personal/service.yaml", "immich", "is declared in its own directory")]
    [InlineData("personal/immich/compose/service.yaml", "immich", "is declared in its own directory")]
    [InlineData("personal/immich/service.yaml", "photos", "named 'photos', but its directory is 'immich'")]
    public void From_RefusesAServiceOutOfItsPlace(string file, string name, string expected) =>
        Errors(Wolfe.Lab.Domain.Catalog.Catalog.Read([Service(file, name)], [])).ShouldHaveSingleItem().ShouldContain(expected);

    // Told to each, since each is a document someone's check is judging.
    [Fact]
    public void From_RefusesTwoServicesOfOneName() =>
        Errors(Wolfe.Lab.Domain.Catalog.Catalog.Read([Service("personal/mail/service.yaml", "mail"), Service("platform/mail/service.yaml", "mail")], []))
            .ShouldBe([
                "personal/mail/service.yaml: another service is named 'mail' too (platform/mail/service.yaml); a service's name is the lab's, not its area's.",
                "platform/mail/service.yaml: another service is named 'mail' too (personal/mail/service.yaml); a service's name is the lab's, not its area's."
            ]);

    [Fact]
    public void OfComponentIn_HoldsACheckToTheProblemsOfItsOwnDirectory()
    {
        var reading = Wolfe.Lab.Domain.Catalog.Catalog.Read([Service("personal/immich/service.yaml", "immich")], [
            Component("personal/immich/compose/component.yaml", "backup"),
            Component("personal/immich/backup/component.yaml", dependsOn: ["nothing"])
        ]);

        reading.IsError.ShouldBeTrue();
        reading.Errors.Count.ShouldBe(2);
        reading.OfComponentIn(RepositoryPath.From("personal/immich/compose")).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("named 'backup'");
        reading.OfComponentIn(RepositoryPath.From("personal/immich/comp")).IsSuccess.ShouldBeTrue();
        reading.Partial.ComponentAt(RepositoryPath.From("personal/immich/backup")).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("personal/immich/compose/component.yaml", "backup", "named 'backup', but its directory is 'compose'")]
    [InlineData("personal/immich/vaults.yaml", null, "needs a 'name'")]
    [InlineData("personal/photos/compose/component.yaml", null, "personal/photos/ declares none")]
    [InlineData("personal/immich/compose/deep/component.yaml", null, "in a directory of its own within it")]
    public void From_RefusesAComponentOutOfItsPlace(string file, string? name, string expected) =>
        Errors(Wolfe.Lab.Domain.Catalog.Catalog.Read([Service("personal/immich/service.yaml", "immich")], [Component(file, name)]))
            .ShouldHaveSingleItem().ShouldContain(expected);

    [Fact]
    public void From_RefusesTwoComponentsOfOneNameInAService() =>
        Errors(Wolfe.Lab.Domain.Catalog.Catalog.Read([Service("personal/immich/service.yaml", "immich")],
                [Component("personal/immich/compose/component.yaml"), Component("personal/immich/component.yaml", "compose")]))
            .ShouldHaveSingleItem().ShouldContain("'compose' is declared already");

    [Fact]
    public void From_HoldsEveryDependencyToLikeAndToWhatIsDeclared()
    {
        var errors = Errors(Wolfe.Lab.Domain.Catalog.Catalog.Read(
            [Service("personal/immich/service.yaml", "immich", "garage"), Service("personal/mail/service.yaml", "mail")],
            [
                Component("personal/immich/compose/component.yaml", dependsOn: ["compose"]),
                Component("personal/mail/watcher/component.yaml", dependsOn: ["bridge"])
            ]));

        errors.ShouldContain(error => error.Contains("depends on the service 'garage', which the lab does not declare"));
        errors.ShouldContain(error => error.Contains("'compose' depends on itself"));
        errors.ShouldContain(error => error.Contains("depends on 'bridge', which its service does not declare"));
        errors.Count.ShouldBe(3);
    }
}
