using System.Xml.Linq;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Workflows.DotNetTool.Steps;

namespace Wolfe.Lab.Tests.Workflows.DotNetTool.Steps;

// Against a real repository: what the count includes is the whole point.
public class ComputeVersionTests : IDisposable
{
    private readonly DirectoryInfo _repository = Directory.CreateTempSubdirectory("lab-version-");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public ComputeVersionTests()
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(_repository.FullName));
        Git("init", "-q", "-b", "main");
        Git("config", "user.email", "test@example.com");
        Git("config", "user.name", "Test");
    }

    public void Dispose() => _repository.Delete(recursive: true);

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

    private async Task<string?> Compute()
    {
        var result = await new ComputeVersion(new ProcessCommandRunner(), _fileSystem, ShippedInputs.For("src/Tool/Tool.csproj", []), Substitute.For<IWorkflowLog>())
            .Run(TestContext.Current.CancellationToken);
        result.IsFailure.ShouldBeFalse();
        return XDocument.Load(Path.Combine(_repository.FullName, ComputeVersion.VersionFile)).Descendants(ComputeVersion.VersionProperty).Single().Value;
    }

    [Fact]
    public async Task Run_CountsOnlyTheCommitsThatChangedWhatShips()
    {
        Commit("src/Tool/Program.cs", "one");
        Commit("Directory.Packages.props", "<Project />");
        Commit("README.md", "docs change nothing that ships");
        Commit("tests/ToolTests.cs", "nor do tests");

        (await Compute()).ShouldBe("1.0.2");

        Commit("src/Tool/Program.cs", "two");

        (await Compute()).ShouldBe("1.0.3");
    }

    [Fact]
    public async Task Run_GoesOnCountingAcrossAMove()
    {
        Commit("old/src/Tool/Program.cs", "one");
        Commit("old/src/Tool/Program.cs", "two");
        Git("mv", "old", "new");
        Git("commit", "-q", "-m", "move");

        // The component is where it lives now; what it was is named from the checkout's root.
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(Path.Combine(_repository.FullName, "new")));
        var result = await new ComputeVersion(new ProcessCommandRunner(), _fileSystem, ShippedInputs.For("src/Tool/Tool.csproj", ["old/src/Tool"]), Substitute.For<IWorkflowLog>())
            .Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        XDocument.Load(Path.Combine(_repository.FullName, "new", ComputeVersion.VersionFile)).Descendants(ComputeVersion.VersionProperty).Single().Value
            .ShouldBe("1.0.3");
    }

    [Fact]
    public async Task Run_RefusesAShallowCheckout()
    {
        var commands = Substitute.For<ICommandRunner>();
        commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "true\n", ""));

        var result = await new ComputeVersion(commands, _fileSystem, ShippedInputs.For("src/Tool/Tool.csproj", []), Substitute.For<IWorkflowLog>())
            .Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        File.Exists(Path.Combine(_repository.FullName, ComputeVersion.VersionFile)).ShouldBeFalse();
    }
}
