using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Workflows.Caddy.Models;
using Wolfe.Lab.Build.Workflows.Caddy.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Caddy.Steps;

public class IssueCertificateTests
{
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();
    private readonly PhysicalDirectory _store = new("/Users/tom/Docker/caddy/lego");

    public IssueCertificateTests() =>
        _secrets.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<string>().StartsWith("op://", StringComparison.Ordinal) ? "t0ken" : call.Arg<string>());

    private CertificateRequest Request(string? wait = "90s") => new(
        "goacme/lego:v5.4.0",
        "tom@example.com",
        ["*.twolfe.dev"],
        "netlify",
        new Dictionary<string, string> { ["NETLIFY_TOKEN"] = "op://Wolfe.Lab/netlify-pat/credential", ["LEGO_REGION"] = "eu" },
        wait,
        _store);

    [Fact]
    public async Task Run_StartsLegoWithTheStateMountedAndTheProviderInItsEnvironment()
    {
        var result = await new IssueCertificate(_docker, _secrets, Request(), Substitute.For<IWorkflowLog>()).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        var run = (ContainerRun)_docker.ReceivedCalls().Single().GetArguments()[0]!;
        run.Image.ShouldBe("goacme/lego:v5.4.0");
        run.Arguments.ShouldBe(["--log.format", "text", "run", "--accept-tos", "--email", "tom@example.com", "--dns", "netlify", "--domains", "*.twolfe.dev", "--path", "/state", "--dns.propagation.wait", "90s"]);
        run.Mounts.ShouldHaveSingleItem().ShouldBe(new BindMount(_store, IssueCertificate.MountPoint));
        run.Environment["NETLIFY_TOKEN"].ShouldBe("t0ken");
        run.Environment["LEGO_REGION"].ShouldBe("eu");
        run.Network.ShouldBeNull();
    }

    [Fact]
    public async Task Run_LeavesLegoToItsOwnDnsCheckWhenNoWaitIsDeclared()
    {
        await new IssueCertificate(_docker, _secrets, Request(wait: null), Substitute.For<IWorkflowLog>()).Run(TestContext.Current.CancellationToken);

        var run = (ContainerRun)_docker.ReceivedCalls().Single().GetArguments()[0]!;
        run.Arguments.ShouldNotContain("--dns.propagation.wait");
    }
}
