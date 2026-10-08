using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Application.Caddy;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Caddy;
using Wolfe.Lab.Infrastructure.Releases;
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

    // Caddy runs with its deployment's Caddyfile, through the install root it mounts at /lab.
    [Fact]
    public async Task Run_IsTheCaddyTheRoutesArePartOf()
    {
        var result = await Resolve(Routes("proxy"));

        result.Value.ShouldBe(new CaddyInstance("caddy", "/lab/caddy-proxy/Caddyfile"));
    }

    [Fact]
    public async Task Run_RefusesAWholeTheServiceDoesNotRunAsADockerComponent()
    {
        var result = await Resolve(Routes("certs"));

        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("'certs'");
    }

    [Fact]
    public async Task Run_IsRittenJsonsCaddyWhileTheRoutesArePartOfNone() =>
        (await Resolve(Routes(null))).Value.ShouldBe(new CaddyInstance("legacy", "/Caddyfile"));

    private static ServiceCatalog Routes(string? partOf)
    {
        var catalog = Catalogs.Of("network/caddy/compose", Catalogs.Docker("proxy", "caddy", ComponentKind.Proxy));
        var service = catalog.Services.ShouldHaveSingleItem();
        Catalogs.Add(service, Source("certs"), Catalogs.Definition("certs", ComponentKind.Certificate, WorkflowName.Tofu, partOf: "proxy")).Value.ShouldNotBeNull();
        Catalogs.Add(service, Source("routes"), Catalogs.Definition("routes", ComponentKind.Proxy, WorkflowName.CaddyRoutes, partOf)).Value.ShouldNotBeNull();
        return catalog;
    }

    private static DocumentSource Source(string directory) => new(RepositoryPath.From($"network/caddy/{directory}/component.yaml"));

    private Task<StepResult<CaddyInstance>> Resolve(ServiceCatalog catalog) =>
        new ResolveCaddy(new DeclaredComponents(_git, _fileSystem), new CaddyOptions { Container = "legacy", Caddyfile = "/Caddyfile" },
                Options.Create(new LabDirectories()), Substitute.For<IWorkflowLog>())
            .Run(catalog, TestContext.Current.CancellationToken);
}
