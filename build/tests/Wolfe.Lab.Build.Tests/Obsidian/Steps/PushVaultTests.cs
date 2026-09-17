using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Build.Git;
using Wolfe.Lab.Build.Obsidian.Models;
using Wolfe.Lab.Build.Obsidian.Steps;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Tests.Obsidian.Steps;

public class PushVaultTests
{
    private static readonly SecretReference Token = SecretReference.From("op://Wolfe.Lab/forgejo-obsidian-token/credential");
    private readonly IGit _repository = Substitute.For<IGit>();
    private readonly ISecrets _secrets = Substitute.For<ISecrets>();
    private readonly Vault _vault = new("main", new PhysicalDirectory("/tmp/vault"), RepositoryUrl.From("http://forgejo/obsidian-main.git"));
    private readonly PushVault _step;

    public PushVaultTests()
    {
        var git = Substitute.For<IGit>();
        git.InRepository(Arg.Any<IDirectory>()).Returns(_repository);
        _repository.CurrentBranch(Arg.Any<CancellationToken>()).Returns("main");
        _secrets.Read(Token, Arg.Any<CancellationToken>()).Returns("t0ken");
        _step = new PushVault(git, _secrets, new PushCredential(GitUsername.From("tom-wolfe"), Token), Substitute.For<IWorkflowLog>());
    }

    [Fact]
    public async Task Run_LeavesTheSecretAloneWhenThereIsNothingToPush()
    {
        _repository.Upstream(Arg.Any<CancellationToken>()).Returns("origin/main");
        _repository.CommitsAhead("origin/main", Arg.Any<CancellationToken>()).Returns(0);

        var result = await _step.Run(_vault, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _secrets.DidNotReceiveWithAnyArgs().Read(Token, TestContext.Current.CancellationToken);
        await _repository.DidNotReceiveWithAnyArgs().Push(null!, null!, ct: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_PushesWithTheTokenWhenAhead()
    {
        _repository.Upstream(Arg.Any<CancellationToken>()).Returns("origin/main");
        _repository.CommitsAhead("origin/main", Arg.Any<CancellationToken>()).Returns(2);

        await _step.Run(_vault, TestContext.Current.CancellationToken);

        await _repository.Received().Push("origin", "main", new GitCredential("tom-wolfe", "t0ken"), false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_SetsTheUpstreamOnTheFirstPush()
    {
        _repository.Upstream(Arg.Any<CancellationToken>()).Returns((string?)null);

        await _step.Run(_vault, TestContext.Current.CancellationToken);

        await _repository.Received().Push("origin", "main", new GitCredential("tom-wolfe", "t0ken"), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_FailsWhenHeadIsDetached()
    {
        _repository.CurrentBranch(Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _step.Run(_vault, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        await _repository.DidNotReceiveWithAnyArgs().Push(null!, null!, ct: TestContext.Current.CancellationToken);
    }
}
