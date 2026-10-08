using Ritten.Docker;
using Wolfe.Lab.Application.Workflows.CaddyCertificates.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Caddy;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Domain.Secrets;

namespace Wolfe.Lab.Tests.Application.Workflows.CaddyCertificates.Steps;

public class IssueCertificateTests
{
    private const string Store = "/Users/tom/Docker/caddy/lego";

    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();

    public IssueCertificateTests() =>
        _secrets.Resolve("op://Wolfe.Lab/netlify-pat/credential", Arg.Any<CancellationToken>()).Returns("t0ken");

    private static CaddyCertificatesComponent Certificate(string? wait = "90s")
    {
        var issuer = new CertificateIssuer("goacme/lego:v5.4.0", "tom@example.com", "netlify", HostPath.From(Store))
        {
            Environment = new Dictionary<string, SecretReference> { ["NETLIFY_TOKEN"] = SecretReference.From("op://Wolfe.Lab/netlify-pat/credential") },
            PropagationWait = wait
        };
        return CaddyCertificatesComponent.Create(new DocumentSource(RepositoryPath.From("network/caddy/certs/component.yaml")), ComponentName.From("certs"),
            ComponentKind.Certificate, ["*.twolfe.dev"], issuer).Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task Run_StartsLegoWithTheStateMountedAndTheProviderInItsEnvironment()
    {
        var result = await new IssueCertificate(_docker, _secrets, Substitute.For<IWorkflowLog>()).Run(Certificate(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        var run = (ContainerRun)_docker.ReceivedCalls().Single().GetArguments()[0]!;
        run.Image.ShouldBe("goacme/lego:v5.4.0");
        run.Arguments.ShouldBe(["--log.format", "text", "run", "--accept-tos", "--email", "tom@example.com", "--dns", "netlify", "--domains", "*.twolfe.dev", "--path", "/state", "--dns.propagation.wait", "90s"]);
        var mount = run.Mounts.ShouldHaveSingleItem();
        (mount.Host.AbsolutePath, mount.Container).ShouldBe((Store, IssueCertificate.MountPoint));
        run.Environment["NETLIFY_TOKEN"].ShouldBe("t0ken");
        run.Network.ShouldBeNull();
    }

    [Fact]
    public async Task Run_LeavesLegoToItsOwnDnsCheckWhenNoWaitIsDeclared()
    {
        await new IssueCertificate(_docker, _secrets, Substitute.For<IWorkflowLog>()).Run(Certificate(wait: null), TestContext.Current.CancellationToken);

        var run = (ContainerRun)_docker.ReceivedCalls().Single().GetArguments()[0]!;
        run.Arguments.ShouldNotContain("--dns.propagation.wait");
    }
}
