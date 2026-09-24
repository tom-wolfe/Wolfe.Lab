using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Clients.Agents.Steps;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Tests.Clients.Agents.Steps;

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

    private Task<StepResult<AgentPlan>> Resolve(params (string Name, AgentSettings Settings)[] agents) =>
        new ResolveAgents(new AgentDeclarations(agents.ToDictionary(a => a.Name, a => a.Settings)), _secrets, Substitute.For<IWorkflowLog>())
            .Run(TestContext.Current.CancellationToken);

    [Fact]
    public async Task Run_ResolvesWhatTheNodeCanActuallyRun()
    {
        var result = await Resolve(("ollama", new AgentSettings { Program = HostPath.From(Program()), Arguments = ["serve"] }));

        var agent = result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem();
        agent.Label.Value.ShouldBe("dev.twolfe.ollama");
        agent.Arguments.ShouldBe(["serve"]);
    }

    [Fact]
    public async Task Run_StampsTheAgentWithTheProgramItFound()
    {
        var program = Program();

        var result = await Resolve(("ollama", new AgentSettings { Program = HostPath.From(program) }));

        result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem()
            .ProgramStamp.ShouldBe(File.GetLastWriteTimeUtc(program));
    }

    [Fact]
    public async Task Run_ResolvesVaultReferencesAndLeavesEverythingElse()
    {
        var result = await Resolve(("beszel-agent", new AgentSettings
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
        var result = await Resolve(("beszel-agent", new AgentSettings
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
        var result = await Resolve(("ollama", new AgentSettings { Program = HostPath.From(Path.Combine(_node.FullName, "absent")) }));

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("not on this node");
    }

    [Fact]
    public async Task Run_RefusesAnAgentThatNamesNoProgram()
    {
        var result = await Resolve(("ollama", new AgentSettings()));

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("'program'");
    }

    [Fact]
    public async Task Run_RefusesAKeyThatCannotBeALabel()
    {
        var result = await Resolve(("not a name", new AgentSettings { Program = HostPath.From(Program()) }));

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("cannot name an agent");
    }

    [Fact]
    public async Task Run_ReportsEverySlicesProblemAtOnce()
    {
        var result = await Resolve(
            ("ollama", new AgentSettings()),
            ("watcher", new AgentSettings { Program = HostPath.From(Path.Combine(_node.FullName, "absent")) }));

        result.Outcome.Errors.ShouldNotBeNull().Count.ShouldBe(2);
    }

    [Fact]
    public async Task Run_OrdersTheAgentsSoAConvergeIsRepeatable()
    {
        var result = await Resolve(
            ("watcher", new AgentSettings { Program = HostPath.From(Program("watcher")) }),
            ("ollama", new AgentSettings { Program = HostPath.From(Program()) }));

        result.Value.ShouldNotBeNull().Agents.Select(a => a.Label.Name).ShouldBe(["ollama", "watcher"]);
    }

    [Fact]
    public async Task Run_RefusesAHomePathTheProcessWouldReceiveLiterally()
    {
        var result = await Resolve(("ollama", new AgentSettings
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
        ResolveAgents.UnexpandedHomePaths(new Dictionary<string, string> { ["V"] = value }).Any().ShouldBe(found);
}
