using Wolfe.Lab.Domain.Catalog;
using Component = Wolfe.Lab.Domain.Components.Component;

namespace Wolfe.Lab.Tests.Domain.Components;

public class ComponentTests
{
    private const string Root = "/lab";

    private static IReadOnlyList<string> Errors(string directory) =>
        [.. Component.From(Root, $"{Root}/{directory}").Errors.ShouldNotBeNull().Select(error => error.Message)];

    [Fact]
    public void From_PlacesAComponentByItsPath()
    {
        var component = Component.From(Root, $"{Root}/personal/mail/watcher").Value.ShouldNotBeNull();

        component.Area.ShouldBe(AreaName.From("personal"));
        component.Service.ShouldBe(ServiceName.From("mail"));
        component.Name.ShouldBe(ComponentName.From("watcher"));
        component.Directory.ShouldBe(RepositoryPath.From("personal/mail/watcher"));
        component.QualifiedName.ShouldBe("personal-mail-watcher");
    }

    [Theory]
    [InlineData("personal/mail")]
    [InlineData("personal/mail/watcher/src")]
    public void From_RefusesAnythingButThreeDirectoriesDown(string directory) =>
        Errors(directory).ShouldHaveSingleItem().ShouldContain("three directories below the checkout's root");

    [Fact]
    public void From_RefusesALocationOutsideTheCheckout() =>
        Component.From(Root, "/elsewhere/a/b").IsError.ShouldBeTrue();

    // The telemetry is labelled with what the path says, so a directory is never spelled into a name.
    [Fact]
    public void From_TakesEachDirectorysNameAsItIs() =>
        Errors("Personal/Mail/watcher").ShouldBe([
            "Personal/Mail/watcher: 'Personal' is not an area: lower case, a letter first, then letters, digits and hyphens.",
            "Personal/Mail/watcher: 'Mail' is not a service's name: lower case, a letter first, then letters, digits and hyphens."
        ]);
}
