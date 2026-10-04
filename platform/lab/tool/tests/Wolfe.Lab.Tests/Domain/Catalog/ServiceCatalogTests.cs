using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Tests.Domain.Catalog;

public class ServiceCatalogTests
{
    private readonly ServiceCatalog _catalog = new();

    private static DocumentSource At(string file) => new(RepositoryPath.From(file));

    private static Service Created(string file, string name, params string[] dependsOn) =>
        Service.Create(At(file), ServiceName.From(name), [.. dependsOn.Select(ServiceName.From)]).Value.ShouldNotBeNull();

    private Result<Service> Add(string file, string name, params string[] dependsOn) => _catalog.Add(Created(file, name, dependsOn));

    private static IReadOnlyList<string> Messages(Result<Service> result) => [.. result.Errors.ShouldNotBeNull().Select(error => error.Message)];

    [Fact]
    public void Add_KeepsEveryServiceInOrder()
    {
        Add("personal/mail/service.yaml", "mail").IsSuccess.ShouldBeTrue();
        Add("media/sonarr/service.yaml", "sonarr").IsSuccess.ShouldBeTrue();

        _catalog.Services.Select(service => service.Directory.Value).ShouldBe(["media/sonarr", "personal/mail"]);
    }

    [Fact]
    public void Add_RefusesANameAnotherAreaHasAlready()
    {
        Add("personal/mail/service.yaml", "mail").IsSuccess.ShouldBeTrue();

        Messages(Add("platform/mail/service.yaml", "mail")).ShouldBe([
            "platform/mail/service.yaml: another service is named 'mail' too (personal/mail/service.yaml); a service's name is the lab's, not its area's."
        ]);
        _catalog.Services.ShouldHaveSingleItem();
    }

    [Fact]
    public void Add_RefusesADirectoryDeclaredAlready()
    {
        Add("personal/mail/service.yaml", "mail").IsSuccess.ShouldBeTrue();

        Messages(_catalog.Add(Created("personal/mail/other.yaml", "mail"))).ShouldHaveSingleItem().ShouldContain("'mail' is declared already, in personal/mail/service.yaml");
    }

    [Fact]
    public void Add_DependsOnlyOnServicesTheCatalogHas()
    {
        Messages(Add("personal/immich/service.yaml", "immich", "garage")).ShouldHaveSingleItem().ShouldContain("depends on the service 'garage'");

        var garage = Add("platform/garage/service.yaml", "garage").Value.ShouldNotBeNull();
        var photos = Add("personal/photos/service.yaml", "photos", "garage").Value.ShouldNotBeNull();

        photos.DependsOn.ShouldBe([ServiceName.From("garage")]);
        _catalog.FindService(photos.DependsOn.Single()).ShouldBeSameAs(garage);
    }

    [Fact]
    public void FindService_IsTheServiceOfTheName_OrNone()
    {
        var mail = Add("personal/mail/service.yaml", "mail").Value.ShouldNotBeNull();

        _catalog.FindService(ServiceName.From("mail")).ShouldBeSameAs(mail);
        _catalog.FindService(ServiceName.From("garage")).ShouldBeNull();
    }

    [Fact]
    public void Add_HoldsEachProblemAsItsWellKnownError() =>
        Add("personal/immich/service.yaml", "immich", "garage").Errors.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<CatalogError>()
            .Problem.ShouldBe(ServiceErrors.DependsOnUndeclared(ServiceName.From("garage")));

    [Theory]
    [InlineData("personal/immich/component.yaml")]
    [InlineData("personal/immich/compose/component.yaml")]
    public void ServiceDeclaring_IsTheServiceWhoseDirectoryHoldsTheFile(string file)
    {
        var immich = Add("personal/immich/service.yaml", "immich").Value.ShouldNotBeNull();

        _catalog.ServiceDeclaring(At(file)).Value.ShouldBeSameAs(immich);
    }

    [Theory]
    [InlineData("personal/photos/compose/component.yaml", "personal/photos/ declares none")]
    [InlineData("personal/immich/compose/deep/component.yaml", "in a directory of its own within it")]
    public void ServiceDeclaring_RefusesAFileNoServiceHolds(string file, string expected)
    {
        Add("personal/immich/service.yaml", "immich").IsSuccess.ShouldBeTrue();

        _catalog.ServiceDeclaring(At(file)).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain(expected);
    }
}
