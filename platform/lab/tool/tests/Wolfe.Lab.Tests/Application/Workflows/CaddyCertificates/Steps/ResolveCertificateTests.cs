using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.CaddyCertificates.Models;
using Wolfe.Lab.Application.Workflows.CaddyCertificates.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Caddy;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Domain.Secrets;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.CaddyCertificates.Steps;

public class ResolveCertificateTests
{
    // As network/caddy/certs/ritten.json has it, until the component declares it.
    private static readonly CaddyCertificatesOptions Legacy = new()
    {
        Image = "goacme/lego:v5.4.0",
        Email = "tom@twolfe.dev",
        Domains = ["*.twolfe.dev"],
        Dns = "netlify",
        Environment = new Dictionary<string, string> { ["NETLIFY_TOKEN"] = "op://Wolfe.Lab/netlify-pat/credential" },
        PropagationWait = "90s",
        Store = HostPath.From("/lab/data/caddy/lego")
    };

    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public ResolveCertificateTests()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory("/checkout"));
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory("/checkout/network/caddy/certs"));
    }

    [Fact]
    public async Task Run_IsTheCertificateRittenJsonDescribesWhileNoneIsDeclared()
    {
        var request = (await Resolve(new ServiceCatalog(), Legacy)).Value.ShouldNotBeNull();

        (request.Image, request.Email, request.Dns, request.PropagationWait).ShouldBe(("goacme/lego:v5.4.0", "tom@twolfe.dev", "netlify", "90s"));
        request.Domains.ShouldBe(["*.twolfe.dev"]);
        request.Environment["NETLIFY_TOKEN"].ShouldBe("op://Wolfe.Lab/netlify-pat/credential");
        request.Store.AbsolutePath.ShouldBe("/lab/data/caddy/lego");
    }

    [Fact]
    public async Task Run_IsTheCertificateTheComponentDeclares()
    {
        var issuer = new CertificateIssuer("goacme/lego:v6", "ops@twolfe.dev", "netlify", HostPath.From("/lab/data/lego"))
        {
            Environment = new Dictionary<string, SecretReference> { ["NETLIFY_TOKEN"] = SecretReference.From("op://Wolfe.Lab/netlify/credential") }
        };
        var catalog = Catalogs.Of("network/caddy/compose", Catalogs.Docker("proxy", "caddy", ComponentKind.Proxy));
        var certificate = CaddyCertificatesComponent.Create(new DocumentSource(RepositoryPath.From("network/caddy/certs/component.yaml")), ComponentName.From("certs"),
            ComponentKind.Certificate, ["*.twolfe.dev"], issuer).Value.ShouldNotBeNull();
        certificate.PartOf = ComponentName.From("proxy");
        catalog.Services.ShouldHaveSingleItem().Add(certificate).Value.ShouldNotBeNull();

        var request = (await Resolve(catalog, Legacy)).Value.ShouldNotBeNull();

        (request.Image, request.Email, request.PropagationWait).ShouldBe(("goacme/lego:v6", "ops@twolfe.dev", null));
        request.Environment["NETLIFY_TOKEN"].ShouldBe("op://Wolfe.Lab/netlify/credential");
        request.Store.AbsolutePath.ShouldBe("/lab/data/lego");
    }

    [Fact]
    public async Task Run_FailsWithNeither() =>
        (await Resolve(new ServiceCatalog(), new CaddyCertificatesOptions())).Outcome.IsFailure.ShouldBeTrue();

    private Task<StepResult<CertificateRequest>> Resolve(ServiceCatalog catalog, CaddyCertificatesOptions legacy) =>
        new ResolveCertificate(new DeclaredComponents(_git, _fileSystem), legacy).Run(catalog, TestContext.Current.CancellationToken);
}
