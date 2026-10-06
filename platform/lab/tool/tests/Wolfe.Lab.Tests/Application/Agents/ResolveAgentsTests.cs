using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Packages;

namespace Wolfe.Lab.Tests.Application.Agents;

public class ResolveAgentsTests : IDisposable
{
    private readonly DirectoryInfo _node = Directory.CreateTempSubdirectory("lab-node-");
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();

    public ResolveAgentsTests() =>
        _secrets.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => $"secret-of-{call.Arg<string>()}");

    public void Dispose() => _node.Delete(recursive: true);

    private string Program(string name = "ollama")
    {
        var path = Path.Combine(_node.FullName, name);
        File.WriteAllText(path, "");
        return path;
    }

    private Task<StepResult<AgentPlan>> Resolve(params (string Name, AgentOptions Options)[] agents) =>
        Resolve(new Dictionary<string, InstalledPackage>(), agents);

    private Task<StepResult<AgentPlan>> Resolve(Dictionary<string, InstalledPackage> packages, params (string Name, AgentOptions Options)[] agents) =>
        new ResolveAgents(_secrets, Resolvers.On(), Substitute.For<IWorkflowLog>())
            .Run(new AgentDeclarations(agents.ToDictionary(a => a.Name, a => a.Options)), new AgentPackages(packages), TestContext.Current.CancellationToken);

    private static InstalledPackage Alloy(string directory, PackageOutcome outcome) =>
        new(new Package("alloy", "grafana/alloy", "1.20.1", "v1.20.1", "alloy-darwin-arm64.zip", "SHA256SUMS"), new Ritten.Engine.FileSystem.PhysicalDirectory(directory), outcome);

    [Fact]
    public async Task Run_ResolvesWhatTheNodeCanActuallyRun()
    {
        var result = await Resolve(("ollama", new AgentOptions { Program = HostPath.From(Program()), Arguments = ["serve"] }));

        var agent = result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem();
        agent.Label.Value.ShouldBe("dev.twolfe.ollama");
        agent.Arguments.ShouldBe(["serve"]);
    }

    [Fact]
    public async Task Run_StampsTheAgentWithTheProgramItFound()
    {
        var program = Program();

        var result = await Resolve(("ollama", new AgentOptions { Program = HostPath.From(program) }));

        result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem()
            .ProgramStamp.ShouldBe(File.GetLastWriteTimeUtc(program));
    }

    [Fact]
    public async Task Run_ResolvesVaultReferencesAndLeavesEverythingElse()
    {
        var result = await Resolve(("beszel-agent", new AgentOptions
        {
            Program = HostPath.From(Program("beszel-agent")),
            Environment = new Dictionary<string, string>
            {
                ["TOKEN"] = "op://Wolfe.Lab/beszel-agent/credential",
                ["HUB_URL"] = "http://localhost:8090"
            }
        }));

        var environment = result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem().Environment;
        environment["TOKEN"].ShouldBe("secret-of-op://Wolfe.Lab/beszel-agent/credential");
        environment["HUB_URL"].ShouldBe("http://localhost:8090");
        await _secrets.Received(1).Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_CarriesWhatTheAgentSupersedes()
    {
        var result = await Resolve(("beszel-agent", new AgentOptions
        {
            Program = HostPath.From(Program("beszel-agent")),
            Supersedes = ["sh.brew.beszel-agent"]
        }));

        result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem().Supersedes.ShouldBe(["sh.brew.beszel-agent"]);
    }

    [Fact]
    public async Task Run_RefusesADeclarationWithNoAgents()
    {
        var result = await Resolve();

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("No agents are declared");
    }

    [Fact]
    public async Task Run_RefusesAnAgentWhoseProgramIsNotOnThisNode()
    {
        var result = await Resolve(("ollama", new AgentOptions { Program = HostPath.From(Path.Combine(_node.FullName, "absent")) }));

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("not on this node");
    }

    [Fact]
    public async Task Run_RefusesAnAgentThatNamesNoProgram()
    {
        var result = await Resolve(("ollama", new AgentOptions()));

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("'program'");
    }

    [Fact]
    public async Task Run_RefusesAKeyThatCannotBeALabel()
    {
        var result = await Resolve(("not a name", new AgentOptions { Program = HostPath.From(Program()) }));

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("cannot name an agent");
    }

    [Fact]
    public async Task Run_ReportsEveryServicesProblemAtOnce()
    {
        var result = await Resolve(
            ("ollama", new AgentOptions()),
            ("watcher", new AgentOptions { Program = HostPath.From(Path.Combine(_node.FullName, "absent")) }));

        result.Outcome.Errors.ShouldNotBeNull().Count.ShouldBe(2);
    }

    [Fact]
    public async Task Run_OrdersTheAgentsSoAConvergeIsRepeatable()
    {
        var result = await Resolve(
            ("watcher", new AgentOptions { Program = HostPath.From(Program("watcher")) }),
            ("ollama", new AgentOptions { Program = HostPath.From(Program()) }));

        result.Value.ShouldNotBeNull().Agents.Select(a => a.Label.Name).ShouldBe(["ollama", "watcher"]);
    }

    [Fact]
    public async Task Run_RefusesAHomePathTheProcessWouldReceiveLiterally()
    {
        var result = await Resolve(("ollama", new AgentOptions
        {
            Program = HostPath.From(Program()),
            Environment = new Dictionary<string, string> { ["OLLAMA_MODELS"] = "~/.ollama/models", ["OLLAMA_HOST"] = "0.0.0.0:11434" }
        }));

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("OLLAMA_MODELS");
    }

    [Theory]
    [InlineData("~/.ollama/models", true)]
    [InlineData("~", true)]
    [InlineData("/Users/tomwolfe/.ollama/models", false)]
    [InlineData("~user", false)]
    [InlineData("a~b", false)]
    public void UnexpandedHomePaths_FindsWhatStartsAtHome(string value, bool found) =>
        Resolvers.On().HomePathsIn(new Dictionary<string, string> { ["V"] = value }).Any().ShouldBe(found);

    [Fact]
    public async Task Run_WritesTheLabsRootsIntoArgumentsAndVariables()
    {
        var result = await Resolve(("alloy", new AgentOptions
        {
            Program = HostPath.From(Program("alloy")),
            Arguments = ["run", "${LAB_ROOT}/alloy/config.alloy", "--storage.path=${LAB_DATA}/alloy"],
            Environment = new Dictionary<string, string> { ["CONFIG"] = "${LAB_ROOT}/alloy", ["KEPT"] = "${HOME}/as-written" }
        }));

        var agent = result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem();
        agent.Arguments.ShouldBe(["run", "/lab/root/alloy/config.alloy", "--storage.path=/lab/data/alloy"]);
        agent.Environment["CONFIG"].ShouldBe("/lab/root/alloy");
        // Only the lab's own two are expanded; anything else reaches the process as written.
        agent.Environment["KEPT"].ShouldBe("${HOME}/as-written");
    }

    [Fact]
    public async Task Run_RunsTheProgramOutOfItsPackage()
    {
        var program = Program("alloy-darwin-arm64");

        var result = await Resolve(new Dictionary<string, InstalledPackage> { ["alloy"] = Alloy(_node.FullName, PackageOutcome.Installed) },
            ("alloy", new AgentOptions { Program = HostPath.From("${PACKAGE}/alloy-darwin-arm64"), Arguments = ["run", "${PACKAGE}/x"] }));

        var agent = result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem();
        agent.Program.Value.ShouldBe(program);
        agent.Arguments.ShouldBe(["run", $"{_node.FullName}/x"]);
    }

    [Fact]
    public async Task Run_RehearsesAnAgentWhosePackageIsNotInstalledYet()
    {
        var result = await Resolve(new Dictionary<string, InstalledPackage> { ["alloy"] = Alloy("/nowhere/alloy/1.20.1", PackageOutcome.WouldInstall) },
            ("alloy", new AgentOptions { Program = HostPath.From("${PACKAGE}/alloy-darwin-arm64") }));

        result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem().Program.Value.ShouldBe("/nowhere/alloy/1.20.1/alloy-darwin-arm64");
    }
}
