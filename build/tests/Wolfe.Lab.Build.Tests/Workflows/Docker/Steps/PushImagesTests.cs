using Ritten.Docker;
using Wolfe.Lab.Build.Clients.Secrets;
using Wolfe.Lab.Build.Values;
using Wolfe.Lab.Build.Workflows.Docker.Models;
using Wolfe.Lab.Build.Workflows.Docker.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Docker.Steps;

public class PushImagesTests
{
    private const string Tag = "code.twolfe.dev/tom-wolfe/ci";

    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();

    public PushImagesTests() =>
        _secrets.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("token");

    private static RegistryPush Push(params string[] tags) => new(
        [.. tags.Select(tag => new DockerImage(tag, "."))],
        new RegistryCredential(GitUsername.From("tom-wolfe"), SecretReference.From("op://Wolfe.Lab/forgejo-packages/credential")));

    private PushImages Step(RegistryPush push, bool dryRun = false) =>
        new(_docker, _secrets, push, new WorkflowJob("image", "build", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>());

    [Theory]
    [InlineData("code.twolfe.dev/tom-wolfe/ci", "code.twolfe.dev")]
    [InlineData("code.twolfe.dev/tom-wolfe/ci:1.2", "code.twolfe.dev")]
    [InlineData("localhost:5000/ci", "localhost:5000")]
    [InlineData("lab/ci", null)]
    [InlineData("ci", null)]
    public void Registry_IsTheHostTheTagNames(string tag, string? expected) =>
        PushImages.Registry(tag).ShouldBe(expected);

    [Fact]
    public async Task Run_PushesNothingWithoutARegistry()
    {
        var result = await Step(RegistryPush.None).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _docker.DidNotReceiveWithAnyArgs().Push("", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_LogsInToTheTagsRegistryThenPushes()
    {
        var result = await Step(Push(Tag)).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        Received.InOrder(() =>
        {
            _docker.Login("code.twolfe.dev", "tom-wolfe", "token", Arg.Any<CancellationToken>());
            _docker.Push(Tag, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Run_RefusesATagThatWouldMeanDockerHub()
    {
        var result = await Step(Push("lab/ci")).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        await _docker.DidNotReceiveWithAnyArgs().Push("", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_ReadsNoSecretOnADryRun()
    {
        var result = await Step(Push(Tag), dryRun: true).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _secrets.DidNotReceiveWithAnyArgs().Resolve("", TestContext.Current.CancellationToken);
        await _docker.DidNotReceiveWithAnyArgs().Push("", TestContext.Current.CancellationToken);
    }
}
