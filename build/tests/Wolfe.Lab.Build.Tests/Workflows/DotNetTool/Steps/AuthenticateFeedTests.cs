using Microsoft.Extensions.Options;
using Ritten.NuGet;
using Wolfe.Lab.Build.Clients.Secrets;
using Wolfe.Lab.Build.Workflows.DotNetTool.Models;
using Wolfe.Lab.Build.Workflows.DotNetTool.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.DotNetTool.Steps;

public class AuthenticateFeedTests
{
    private const string Feed = "https://code.twolfe.dev/api/packages/tom-wolfe/nuget/index.json";
    private const string Reference = "op://Wolfe.Lab/forgejo-packages/credential";

    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();

    public AuthenticateFeedTests() =>
        _secrets.Resolve(Reference, Arg.Any<CancellationToken>()).Returns("the-key");

    private AuthenticateFeed Step(bool dryRun = false) => new(
        Options.Create(new NuGetOptions { Feed = Feed }),
        _secrets,
        new FeedToken(SecretReference.From(Reference)),
        new WorkflowJob("dotnet-tool", "deploy", dryRun, AutoApprove: true),
        Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_ResolvesTheKeyFromTheVault()
    {
        var result = await Step().Run(TestContext.Current.CancellationToken);

        var feed = result.Value.ShouldNotBeNull();
        feed.Url.ShouldBe(Feed);
        feed.ApiKey.ShouldBe("the-key");
    }

    [Fact]
    public async Task Run_ReadsNoSecretOnADryRun()
    {
        var result = await Step(dryRun: true).Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().ApiKey.ShouldBeNull();
        await _secrets.DidNotReceiveWithAnyArgs().Resolve("", TestContext.Current.CancellationToken);
    }
}
