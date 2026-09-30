using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.DotNet;
using Ritten.Git;
using Wolfe.Lab.Workflows.DotNetTool.Jobs;
using Wolfe.Lab.Workflows.DotNetTool.Steps;

namespace Wolfe.Lab.Tests.Workflows.DotNetTool.Steps;

public class TagReleaseTests
{
    private const string Tag = "lab/v1.0.214";

    private readonly IGit _git = Substitute.For<IGit>();

    private Task<StepResult> Run() =>
        new TagRelease(_git, Options.Create(new GitOptions { TagPrefix = DeployJob.TagPrefix, CommitSha = "abc123" }), Substitute.For<IWorkflowLog>())
            .Run(new Project { Name = "Wolfe.Lab", Version = NuGetVersion.Parse("1.0.214") }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Run_TagsTheCommitThePackageWasBuiltFromAndPushesIt()
    {
        await Run();

        await _git.Received().CreateTag(Tag, "abc123", Arg.Any<CancellationToken>());
        await _git.Received().PushTag("origin", Tag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_LeavesATagAlreadyOnTheRemoteAlone()
    {
        _git.RemoteTagExists("origin", Tag, Arg.Any<CancellationToken>()).Returns(true);

        await Run();

        await _git.DidNotReceiveWithAnyArgs().CreateTag("", null, TestContext.Current.CancellationToken);
        await _git.DidNotReceiveWithAnyArgs().PushTag("", "", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_PushesATagARunBeforeCreatedButNeverPushed()
    {
        _git.TagExists(Tag, Arg.Any<CancellationToken>()).Returns(true);

        await Run();

        await _git.DidNotReceiveWithAnyArgs().CreateTag("", null, TestContext.Current.CancellationToken);
        await _git.Received().PushTag("origin", Tag, Arg.Any<CancellationToken>());
    }
}
