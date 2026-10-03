using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Tests.Domain.Catalog.Services;

public class ServiceTests
{
    private readonly Service _immich = Catalogs.AddService(new ServiceCatalog(), "personal/immich").Value.ShouldNotBeNull();

    private static Result<Service> Create(string file, string name, params string[] dependsOn) =>
        Service.Create(new DocumentSource(RepositoryPath.From(file)), ServiceName.From(name), [.. dependsOn.Select(ServiceName.From)]);

    private Result<Component> Add(string name, string? partOf = null, string[]? dependsOn = null, string file = "personal/immich/compose/component.yaml",
        WorkflowName? workflow = null) =>
        Catalogs.Add(_immich, new DocumentSource(RepositoryPath.From(file)), new Catalogs.Declaration(name, ComponentKind.App, workflow ?? WorkflowName.Restic, partOf, dependsOn));

    private static IReadOnlyList<string> Messages<T>(Result<T> result) where T : class => [.. result.Errors.ShouldNotBeNull().Select(error => error.Message)];

    [Fact]
    public void Create_PlacesItInItsArea() =>
        Create("personal/mail/service.yaml", "mail").Value.ShouldNotBeNull().Area.ShouldBe(AreaName.From("personal"));

    [Theory]
    [InlineData("personal/service.yaml", "immich", "is declared in its own directory")]
    [InlineData("personal/immich/compose/service.yaml", "immich", "is declared in its own directory")]
    [InlineData("personal/immich/service.yaml", "photos", "named 'photos', but its directory is 'immich'")]
    [InlineData("Personal/immich/service.yaml", "immich", "its area's directory, 'Personal', is not a name")]
    public void Create_RefusesOneOutOfItsPlace(string file, string name, string expected) =>
        Messages(Create(file, name)).ShouldHaveSingleItem().ShouldContain(expected);

    [Fact]
    public void Create_RefusesOneThatDependsOnItself() =>
        Messages(Create("personal/mail/service.yaml", "mail", "mail")).ShouldHaveSingleItem().ShouldContain("'mail' depends on itself");

    [Fact]
    public void Add_MakesTheComponentItsOwn_InTheDirectoryItIsDeclaredIn()
    {
        var server = Add("server").Value.ShouldNotBeNull();
        var vault = Add("main-vault", file: "personal/immich/vaults.yaml").Value.ShouldNotBeNull();

        server.Service.ShouldBeSameAs(_immich);
        server.Directory.ShouldBe(RepositoryPath.From("personal/immich/compose"));
        vault.Directory.ShouldBe(RepositoryPath.From("personal/immich"));
        _immich.Components.Select(component => component.Name.Value).ShouldBe(["main-vault", "server"]);
    }

    [Fact]
    public void Add_RefusesAComponentOutsideItsDirectory() =>
        Messages(Add("server", file: "personal/mail/compose/component.yaml")).ShouldHaveSingleItem().ShouldContain("in a directory of its own within it");

    [Fact]
    public void Add_RefusesANameItHasAlready()
    {
        Add("server").IsSuccess.ShouldBeTrue();

        Messages(Add("server", file: "personal/immich/vaults.yaml")).ShouldHaveSingleItem()
            .ShouldContain("'server' is declared already, in personal/immich/compose/component.yaml");
        _immich.Components.ShouldHaveSingleItem();
    }

    [Fact]
    public void Add_TakesWhatAComponentIsPartOfAndDependsOnOnlyFromItsComponents()
    {
        Messages(Add("database", partOf: "postgres", dependsOn: ["cache"])).ShouldBe([
            "personal/immich/compose/component.yaml: is part of 'postgres', which its service does not declare, or which is part of this one in turn; a component is part only of a component of its own service.",
            "personal/immich/compose/component.yaml: depends on 'cache', which its service does not declare, or which depends on this one in turn; a component depends only on components of its own service."
        ]);
        _immich.Components.ShouldBeEmpty();
    }

    [Fact]
    public void Add_KeepsWhatAComponentNames_AndItsHostIsFoundThroughThem()
    {
        var postgres = Add("postgres", workflow: WorkflowName.Docker).Value.ShouldNotBeNull();
        var database = Add("database", partOf: "postgres").Value.ShouldNotBeNull();
        var dump = Add("dump", partOf: "database", dependsOn: ["postgres"]).Value.ShouldNotBeNull();

        dump.PartOf.ShouldBe(ComponentName.From("database"));
        dump.DependsOn.ShouldBe([ComponentName.From("postgres")]);
        _immich.FindComponent(ComponentName.From("database")).ShouldBeSameAs(database);
        dump.Host.ShouldBeSameAs(postgres);
        postgres.Host.ShouldBeSameAs(postgres);
    }

    [Fact]
    public void FindComponent_IsTheComponentOfTheName_OrNone()
    {
        var server = Add("server").Value.ShouldNotBeNull();

        _immich.FindComponent(ComponentName.From("server")).ShouldBeSameAs(server);
        _immich.FindComponent(ComponentName.From("cache")).ShouldBeNull();
    }

    [Fact]
    public void Add_RefusesAComponentAnotherServiceHas()
    {
        var server = Add("server").Value.ShouldNotBeNull();
        var other = Catalogs.AddService(new ServiceCatalog(), "personal/immich").Value.ShouldNotBeNull();

        Should.Throw<InvalidOperationException>(() => other.Add(server));
    }
}
