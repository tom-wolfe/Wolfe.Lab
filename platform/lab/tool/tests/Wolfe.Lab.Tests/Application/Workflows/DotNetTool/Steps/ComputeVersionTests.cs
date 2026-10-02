using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Application.Workflows.DotNetTool.Jobs;
using Wolfe.Lab.Application.Workflows.DotNetTool.Steps;

namespace Wolfe.Lab.Tests.Application.Workflows.DotNetTool.Steps;

// Against a real repository: what counts as a change since the last release is the whole point.
public class ComputeVersionTests : IDisposable
{
    private readonly DirectoryInfo _repository = Directory.CreateTempSubdirectory("lab-version-");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IGit _git = Substitute.For<IGit>();

    public ComputeVersionTests()
    {
        Git("init", "-q", "-b", "main");
        Git("config", "user.email", "test@example.com");
        Git("config", "user.name", "Test");
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory(_repository.FullName));
        At("platform/lab/tool");
    }

    public void Dispose() => _repository.Delete(recursive: true);

    // Not created: a move into it must find nothing there, as it would in a real checkout.
    private void At(string component) =>
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(Path.Combine(_repository.FullName, component)));

    private void Git(params string[] arguments) =>
        new ProcessCommandRunner().Run(Command.Create("git").WithArguments(arguments).InDirectory(_repository.FullName).ThrowOnError()).GetAwaiter().GetResult();

    private void Commit(string path, string content)
    {
        var file = Path.Combine(_repository.FullName, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file) ?? _repository.FullName);
        File.WriteAllText(file, content);
        Git("add", "-A");
        Git("commit", "-q", "-m", path);
    }

    private void Release(string version) => Git("tag", DeployJob.TagPrefix + version);

    private ComputeVersion Step(ICommandRunner? commands = null) =>
        new(commands ?? new ProcessCommandRunner(), _git, _fileSystem, ShippedInputs.For("src/Tool/Tool.csproj"),
            Options.Create(new GitOptions { TagPrefix = DeployJob.TagPrefix }), Substitute.For<IWorkflowLog>());

    private async Task<string> Compute()
    {
        (await Step().Run(TestContext.Current.CancellationToken)).IsFailure.ShouldBeFalse();
        return XDocument.Load(Path.Combine(_fileSystem.ProjectRoot.AbsolutePath, ComputeVersion.VersionFile))
            .Descendants(ComputeVersion.VersionProperty).Single().Value;
    }

    [Fact]
    public async Task Run_StartsAtTheFirstRelease()
    {
        Commit("platform/lab/tool/src/Tool/Program.cs", "one");

        (await Compute()).ShouldBe("1.0.1");
    }

    [Fact]
    public async Task Run_KeepsTheLastReleaseWhenNothingThatShipsHasChanged()
    {
        Commit("platform/lab/tool/src/Tool/Program.cs", "one");
        Release("1.0.52");
        Commit("platform/lab/README.md", "docs ship nothing");
        Commit("platform/lab/tool/tests/ToolTests.cs", "nor do tests");

        (await Compute()).ShouldBe("1.0.52");
    }

    [Theory]
    [InlineData("platform/lab/tool/src/Tool/Program.cs")]
    [InlineData("platform/lab/tool/Directory.Packages.props")]
    [InlineData("global.json")]
    public async Task Run_ReleasesTheNextVersionWhenWhatShipsHasChanged(string path)
    {
        Commit(path, "one");
        Release("1.0.52");
        Commit(path, "two");

        (await Compute()).ShouldBe("1.0.53");
    }

    // The tool is built from the projects it references, and theirs: Domain through Application.
    private void CommitProjects()
    {
        Commit("platform/lab/tool/src/Tool/Tool.csproj", """
            <Project><ItemGroup><ProjectReference Include="..\Tool.Application\Tool.Application.csproj" /></ItemGroup></Project>
            """);
        Commit("platform/lab/tool/src/Tool.Application/Tool.Application.csproj", """
            <Project><ItemGroup><ProjectReference Include="..\Tool.Domain\Tool.Domain.csproj" /></ItemGroup></Project>
            """);
        Commit("platform/lab/tool/src/Tool.Domain/Tool.Domain.csproj", "<Project />");
        Commit("platform/lab/tool/src/Other/Other.csproj", "<Project />");
    }

    [Theory]
    [InlineData("platform/lab/tool/src/Tool.Application/Steps.cs")]
    [InlineData("platform/lab/tool/src/Tool.Domain/Model.cs")]
    public async Task Run_ReleasesTheNextVersionWhenAProjectTheToolIsBuiltFromHasChanged(string path)
    {
        CommitProjects();
        Commit(path, "one");
        Release("1.0.52");
        Commit(path, "two");

        (await Compute()).ShouldBe("1.0.53");
    }

    [Fact]
    public async Task Run_KeepsTheLastReleaseWhenOnlyAProjectTheToolDoesNotReferenceHasChanged()
    {
        CommitProjects();
        Release("1.0.52");
        Commit("platform/lab/tool/src/Other/Other.cs", "not in the package");

        (await Compute()).ShouldBe("1.0.52");
    }

    [Fact]
    public async Task Run_CountsAMoveAsOneChangeAndCarriesOn()
    {
        // The release was tagged where the tool used to live; the move changes what ships, once.
        Commit("build/src/Tool/Program.cs", "one");
        Release("1.0.52");
        Directory.CreateDirectory(Path.Combine(_repository.FullName, "platform", "lab"));
        Git("mv", "build", "platform/lab/tool");
        Git("commit", "-q", "-m", "move");

        (await Compute()).ShouldBe("1.0.53");
    }

    [Fact]
    public async Task Run_CountsFromTheHighestReleaseAndOnlyItsOwnTags()
    {
        Commit("platform/lab/tool/src/Tool/Program.cs", "one");
        Release("1.0.9");
        Release("1.0.10");
        Git("tag", "history/2026-09-30-first-restore");
        Commit("platform/lab/tool/src/Tool/Program.cs", "two");

        (await Compute()).ShouldBe("1.0.11");
    }

    [Fact]
    public async Task Run_RefusesAShallowCheckout()
    {
        var commands = Substitute.For<ICommandRunner>();
        commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "true\n", ""));

        (await Step(commands).Run(TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        File.Exists(Path.Combine(_fileSystem.ProjectRoot.AbsolutePath, ComputeVersion.VersionFile)).ShouldBeFalse();
    }
}
