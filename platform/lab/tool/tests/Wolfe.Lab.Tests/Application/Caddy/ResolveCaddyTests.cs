using Microsoft.Extensions.Options;
using Wolfe.Lab.Application.Caddy;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Caddy;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Caddy;

public class ResolveCaddyTests
{
    // Caddy runs with its deployment's Caddyfile, through the install root it mounts at /lab.
    [Fact]
    public void Run_IsTheCaddyTheRoutesArePartOf() =>
        Resolve("proxy").Value.ShouldBe(new CaddyInstance("caddy", "/lab/caddy-proxy/Caddyfile"));

    [Fact]
    public void Run_RefusesAWholeTheServiceDoesNotRunAsADockerComponent() =>
        Resolve("certs").Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("'certs'");

    [Fact]
    public void Run_RefusesRoutesThatArePartOfNoCaddy() =>
        Resolve(null).Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("partOf");

    private static StepResult<CaddyInstance> Resolve(string? partOf)
    {
        var catalog = Catalogs.Of("network/caddy/compose", Catalogs.Docker("proxy", "caddy", ComponentKind.Proxy));
        var service = catalog.Services.ShouldHaveSingleItem();
        Catalogs.Add(service, Source("certs"), Catalogs.Definition("certs", ComponentKind.Certificate, WorkflowName.Tofu, partOf: "proxy")).Value.ShouldNotBeNull();
        Catalogs.Add(service, Source("routes"), Catalogs.Definition("routes", ComponentKind.Proxy, WorkflowName.CaddyRoutes, partOf)).Value.ShouldNotBeNull();
        var routes = catalog.DeploymentUnitAt(RepositoryPath.From("network/caddy/routes")).ShouldNotBeNull().Value.ShouldNotBeNull();
        return new ResolveCaddy(Options.Create(new LabDirectories()), Substitute.For<IWorkflowLog>()).Run(catalog, routes);
    }

    private static DocumentSource Source(string directory) => new(RepositoryPath.From($"network/caddy/{directory}/component.yaml"));
}
