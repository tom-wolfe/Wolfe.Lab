using Wolfe.Lab.Application.Workflows.CaddyCertificates.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Caddy;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.CaddyCertificates.Steps;

public class ResolveCertificateTests
{
    [Fact]
    public void Run_IsTheCertificateTheComponentDeclares()
    {
        var issuer = new CertificateIssuer("goacme/lego:v5.4.0", "tom@twolfe.dev", "netlify", HostPath.From("/lab/data/lego"));
        var unit = Catalogs.UnitOf(CaddyCertificatesComponent.Create(new DocumentSource(RepositoryPath.From("network/caddy/certs/component.yaml")), ComponentName.From("certs"),
            ComponentKind.Certificate, ["*.twolfe.dev"], issuer));

        var certificate = new ResolveCertificate(Substitute.For<IWorkflowLog>()).Run(unit).Value.ShouldNotBeNull();

        certificate.Domains.ShouldBe(["*.twolfe.dev"]);
        certificate.Issuer.ShouldBe(issuer);
    }

    [Fact]
    public void Run_FailsInADirectoryTheWorkflowDoesNotOperate() =>
        new ResolveCertificate(Substitute.For<IWorkflowLog>()).Run(Catalogs.Unit("network/caddy/compose", Catalogs.Docker("proxy", "caddy")))
            .Outcome.IsFailure.ShouldBeTrue();
}
