using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Infrastructure.Packages;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Tests.Infrastructure.Packages;

public class GithubPackageInstallerTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-packages-");

    public void Dispose() => _root.Delete(recursive: true);

    private static readonly IOptions<GithubOptions> Sources =
        Options.Create(new GithubOptions { Releases = new Uri("https://github.com/"), Api = new Uri("https://api.github.com/") });

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
        var installer = new GithubPackageInstaller(Clients(http), Sources, Substitute.For<ICommandRunner>(),
            Options.Create(new LabDirectories { Root = new PhysicalDirectory(_root.FullName) }), Substitute.For<IWorkflowLog>());

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

    [Theory]
    [InlineData("MATCH", null)]
    [InlineData("sha256:0000000000000000000000000000000000000000000000000000000000000000", "does not match GitHub's digest")]
    [InlineData(null, "records no SHA-256")]
    public async Task Install_ChecksAPackageWithNoChecksumFileAgainstGitHubsDigest(string? digest, string? failure)
    {
        var body = "#!/bin/sh\necho shellcheck\n"u8.ToArray();
        var actual = "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(body));
        var package = new Package("shellcheck", "koalaman/shellcheck", "0.11.0", "v0.11.0", "shellcheck", null);
        var release = System.Text.Json.JsonSerializer.Serialize(new
        {
            assets = new object[] { digest is null ? new { name = "shellcheck" } : new { name = "shellcheck", digest = digest == "MATCH" ? actual : digest } }
        });
        var installer = new GithubPackageInstaller(Clients(new HttpClient(new ReleaseHandler(body, release))), Sources, Substitute.For<ICommandRunner>(),
            Options.Create(new LabDirectories { Root = new PhysicalDirectory(_root.FullName) }), Substitute.For<IWorkflowLog>());

        if (failure is null)
        {
            var installed = await installer.Install(package, TestContext.Current.CancellationToken);
            installed.Outcome.ShouldBe(PackageOutcome.Installed);
            File.Exists(Path.Combine(installed.Directory.AbsolutePath, "shellcheck")).ShouldBeTrue();
        }
        else
        {
            (await Should.ThrowAsync<InvalidOperationException>(() => installer.Install(package, TestContext.Current.CancellationToken)))
                .Message.ShouldContain(failure);
        }
    }

    private sealed class ReleaseHandler(byte[] asset, string release) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = request.RequestUri!.Host == "api.github.com" ? new StringContent(release) : new ByteArrayContent(asset)
            });
    }

    private static IHttpClientFactory Clients(HttpClient client)
    {
        var clients = Substitute.For<IHttpClientFactory>();
        clients.CreateClient(Arg.Any<string>()).Returns(client);
        return clients;
    }
}
