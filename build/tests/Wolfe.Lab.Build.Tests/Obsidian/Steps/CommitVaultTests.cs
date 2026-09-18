using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Build.Git;
using Wolfe.Lab.Build.Obsidian.Models;
using Wolfe.Lab.Build.Obsidian.Steps;

namespace Wolfe.Lab.Build.Tests.Obsidian.Steps;

public class CommitVaultTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-vault-");
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IGit _repository = Substitute.For<IGit>();
    private readonly Vault _vault;

    public CommitVaultTests()
    {
        _vault = new Vault("main", new PhysicalDirectory(_checkout.FullName), RepositoryUrl.From("http://forgejo/obsidian-main.git"));
        _git.InRepository(Arg.Any<IDirectory>()).Returns(_repository);
        _repository.IsRepository(Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetRemoteUrl("origin", Arg.Any<CancellationToken>()).Returns("http://forgejo/obsidian-main.git");
        _repository.ChangedFiles(".", Arg.Any<CancellationToken>()).Returns([]);
    }

    public void Dispose() => _checkout.Delete(recursive: true);

    private CommitVault Step(params string[] excludes) =>
        new(_git, new VaultExcludes(excludes), Substitute.For<IWorkflowLog>());

    /// <summary>
    /// The client says it is a repository; the directory exists for the exclude file's sake.
    /// </summary>
    private void MakeRepository()
    {
        _repository.IsRepository(Arg.Any<CancellationToken>()).Returns(true);
        Directory.CreateDirectory(Path.Combine(_checkout.FullName, ".git"));
    }

    [Fact]
    public async Task Run_FailsWhenTheCheckoutIsNotARepository()
    {
        var result = await Step().Run(_vault, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        await _repository.DidNotReceiveWithAnyArgs().Stage(null!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_WritesTheExcludeListBeforeCounting()
    {
        MakeRepository();

        await Step(".DS_Store", ".trash/").Run(_vault, TestContext.Current.CancellationToken);

        var exclude = await File.ReadAllLinesAsync(Path.Combine(_checkout.FullName, ".git", "info", "exclude"), TestContext.Current.CancellationToken);
        exclude.ShouldContain(".DS_Store");
        exclude.ShouldContain(".trash/");
        exclude[0].ShouldStartWith("#");
    }

    [Fact]
    public async Task Run_AddsTheRemoteWhenTheCheckoutHasNone()
    {
        MakeRepository();
        _repository.GetRemoteUrl("origin", Arg.Any<CancellationToken>()).Returns((string?)null);

        await Step().Run(_vault, TestContext.Current.CancellationToken);

        await _repository.Received().AddRemote("origin", "http://forgejo/obsidian-main.git", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_RefusesACheckoutThatPushesSomewhereElse()
    {
        MakeRepository();
        _repository.GetRemoteUrl("origin", Arg.Any<CancellationToken>()).Returns("http://forgejo/elsewhere.git");

        var result = await Step().Run(_vault, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("remote set-url origin http://forgejo/obsidian-main.git");
        await _repository.DidNotReceiveWithAnyArgs().Stage(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_CommitsNothingWhenNothingChanged()
    {
        MakeRepository();

        var result = await Step().Run(_vault, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _repository.DidNotReceiveWithAnyArgs().Stage(null!, TestContext.Current.CancellationToken);
        await _repository.DidNotReceiveWithAnyArgs().Commit(null!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_StagesAndCommitsWhatChanged()
    {
        MakeRepository();
        _repository.ChangedFiles(".", Arg.Any<CancellationToken>()).Returns(["a.md", "b.md"]);

        var result = await Step().Run(_vault, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        Received.InOrder(() =>
        {
            _repository.Stage(".", Arg.Any<CancellationToken>());
            _repository.Commit(CommitVault.Message, Arg.Any<CancellationToken>());
        });
    }
}
