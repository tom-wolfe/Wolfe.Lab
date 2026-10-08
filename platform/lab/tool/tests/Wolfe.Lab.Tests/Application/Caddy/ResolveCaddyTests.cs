using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Application.Caddy;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Caddy;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Caddy;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Caddy;

public class ResolveCaddyTests
{
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public ResolveCaddyTests()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory("/checkout"));
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory("/checkout/network/caddy/routes"));
    }

    [Fact]
    public async Task Run_IsTheContainerOfTheComponentTheRoutesReload()
    {
        var result = await Resolve(Routes("proxy"));

        result.Value.ShouldBe(new CaddyInstance("caddy", "/etc/caddy/lab/caddy-proxy/Caddyfile"));
    }

    [Fact]
    public async Task Run_RefusesAComponentTheServiceDoesNotRunCaddyAs()
    {
        var result = await Resolve(Routes("front-door"));

        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("'front-door'");
    }

    [Fact]
    public async Task Run_IsRittenJsonsCaddyWhileTheRoutesNameNone() =>
        (await Resolve(Routes(null))).Value.ShouldBe(new CaddyInstance("legacy", "/Caddyfile"));

    private static ServiceCatalog Routes(string? reloads)
    {
        var catalog = Catalogs.Of("network/caddy/compose", Catalogs.Docker("proxy", "caddy", ComponentKind.Proxy));
        var service = catalog.Services.ShouldHaveSingleItem();
        var routes = CaddyRoutesComponent.Create(new DocumentSource(RepositoryPath.From("network/caddy/routes/component.yaml")), ComponentName.From("routes"), ComponentKind.Proxy).Value.ShouldNotBeNull();
        routes.Reloads = reloads is null ? null : new CaddyReload(ComponentName.From(reloads), "/etc/caddy/lab/caddy-proxy/Caddyfile");
        service.Add(routes).Value.ShouldNotBeNull();
        return catalog;
    }

    private Task<StepResult<CaddyInstance>> Resolve(ServiceCatalog catalog) =>
        new ResolveCaddy(new DeclaredComponents(_git, _fileSystem), new CaddyOptions { Container = "legacy", Caddyfile = "/Caddyfile" }, Substitute.For<IWorkflowLog>())
            .Run(catalog, TestContext.Current.CancellationToken);
}
