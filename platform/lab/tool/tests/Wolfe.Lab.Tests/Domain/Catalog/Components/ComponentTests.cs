using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Tests.Domain.Catalog.Components;

public class ComponentTests
{
    [Fact]
    public void LivesWhereItsServiceDoes()
    {
        var component = Catalogs.Component("personal/mail/compose", "watcher");

        component.Area.ShouldBe(AreaName.From("personal"));
        component.Area.ShouldBe(component.Service.Area);
        component.Service.Name.ShouldBe(ServiceName.From("mail"));
        component.Service.Components.ShouldHaveSingleItem().ShouldBeSameAs(component);
        component.Name.ShouldBe(ComponentName.From("watcher"));
        component.Directory.ShouldBe(RepositoryPath.From("personal/mail/compose"));
        component.QualifiedName.ShouldBe("personal-mail-watcher");
        component.ResourceAttributes.ShouldBe("lab.area=personal,lab.service=mail,lab.component=watcher");
    }

    [Fact]
    public void IsInItsServicesDirectoryBesideItsEntry()
    {
        var obsidian = Catalogs.AddService(new ServiceCatalog(), "personal/obsidian").Value.ShouldNotBeNull();
        var vault = Catalogs.Add(obsidian, new DocumentSource(RepositoryPath.From("personal/obsidian/vaults.yaml")),
            Catalogs.Definition("main-vault", ComponentKind.Backup, WorkflowName.Obsidian)).Value.ShouldNotBeNull();

        vault.Directory.ShouldBe(RepositoryPath.From("personal/obsidian"));
        vault.Host.ShouldBeNull();
        vault.QualifiedName.ShouldBe("personal-obsidian-main-vault");
    }

    [Fact]
    public void IsOneByItsServiceAndName()
    {
        Catalogs.Component("personal/mail/compose", "watcher").ShouldBe(Catalogs.Component("personal/mail/compose", "watcher"));
        Catalogs.Component("personal/mail/compose", "watcher").ShouldNotBe(Catalogs.Component("personal/mail/compose", "bridge"));
    }

    [Theory]
    [InlineData("personal/component.yaml")]
    [InlineData("personal/immich/compose/deep/component.yaml")]
    public void Create_RefusesADeclarationOutOfAnyServicesPlace(string file) =>
        Component.Create(new DocumentSource(RepositoryPath.From(file)), ComponentName.From("server"), ComponentKind.App, WorkflowName.Restic, null, [])
            .Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("in a directory of its own within it");

    [Fact]
    public void Create_RefusesOneThatIsPartOfOrDependsOnItself() =>
        Component.Create(new DocumentSource(RepositoryPath.From("personal/immich/compose/component.yaml")), ComponentName.From("loop"),
                ComponentKind.App, WorkflowName.Restic, ComponentName.From("loop"), [ComponentName.From("loop")])
            .Errors.ShouldNotBeNull().Select(error => error.Message).ShouldBe([
                "personal/immich/compose/component.yaml: 'loop' is part of itself.",
                "personal/immich/compose/component.yaml: 'loop' depends on itself."
            ]);
}
