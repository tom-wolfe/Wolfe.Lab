using Wolfe.Lab.Build.Clients.Packages;

namespace Wolfe.Lab.Build.Tests.Clients.Packages;

public class GithubPackageInstallerTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-packages-");

    public void Dispose() => _root.Delete(recursive: true);

    [Theory]
    [InlineData("abc123  restic_0.19.1_darwin_arm64.bz2", "restic_0.19.1_darwin_arm64.bz2", "abc123")]
    [InlineData("abc123 *beszel-agent_darwin_arm64.tar.gz", "beszel-agent_darwin_arm64.tar.gz", "abc123")]
    [InlineData("abc123  ./ollama-darwin.tgz", "ollama-darwin.tgz", "abc123")]
    [InlineData("ffff  other.zip\nabc123  alloy-darwin-arm64.zip\n", "alloy-darwin-arm64.zip", "abc123")]
    [InlineData("abc123  alloy-darwin-arm64.zip.sig", "alloy-darwin-arm64.zip", null)]
    public void Expected_ReadsEachPublishersChecksumFile(string checksums, string asset, string? expected) =>
        GithubPackageInstaller.Expected(checksums, asset).ShouldBe(expected);

    [Fact]
    public async Task Install_TrustsOnlyAVersionThatWasInstalledWhole()
    {
        var package = new Package("alloy", "grafana/alloy", "1.20.1", "v1.20.1", "alloy-darwin-arm64.zip", "SHA256SUMS");
        var directory = Directory.CreateDirectory(Path.Combine(_root.FullName, "packages", "alloy", "1.20.1"));
        File.WriteAllText(Path.Combine(directory.FullName, GithubPackageInstaller.Complete), "");
        var http = new HttpClient(new RefusingHandler());
        var installer = new GithubPackageInstaller(http, Substitute.For<ICommandRunner>(),
            new WorkflowEnvironment(name => name == "LAB_ROOT" ? _root.FullName : null), Substitute.For<IWorkflowLog>());

        var installed = await installer.Install(package, TestContext.Current.CancellationToken);

        // Present, and without a request: the marker is written last, so it means whole.
        installed.Outcome.ShouldBe(PackageOutcome.Present);
        installed.Directory.AbsolutePath.ShouldBe(directory.FullName);
    }

    private sealed class RefusingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException($"No request was expected: {request.RequestUri}");
    }
}
