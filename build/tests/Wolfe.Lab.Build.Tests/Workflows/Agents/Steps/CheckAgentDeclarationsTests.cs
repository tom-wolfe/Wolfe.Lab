using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Values;
using Wolfe.Lab.Build.Workflows.Agents.Models;
using Wolfe.Lab.Build.Workflows.Agents.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Agents.Steps;

public class CheckAgentDeclarationsTests
{
    private static StepResult Check(params (string Node, string Agent, AgentSettings Settings)[] agents)
    {
        var nodes = agents
            .GroupBy(a => a.Node)
            .ToDictionary(g => g.Key, g => new NodeAgentsSettings { Agents = g.ToDictionary(a => a.Agent, a => a.Settings) });
        return new CheckAgentDeclarations(new AgentsDeclaredPerNode(nodes), Substitute.For<IWorkflowLog>()).Run();
    }

    private static AgentSettings Beszel(string token = "op://Wolfe.Lab/beszel-agent/credential") => new()
    {
        Program = HostPath.From("/opt/homebrew/opt/beszel-agent/bin/beszel-agent"),
        Environment = new Dictionary<string, string> { ["TOKEN"] = token, ["HUB_URL"] = "http://localhost:8090" }
    };

    [Fact]
    public void Run_PassesSoundDeclarations() =>
        Check(("MacMini", "beszel-agent", Beszel()), ("wolfe-pi5", "beszel-agent", Beszel())).IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_RefusesAVaultReferenceThatIsNotOne()
    {
        var result = Check(("MacMini", "beszel-agent", Beszel(token: "op://Wolfe.Lab/beszel-agent")));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("TOKEN");
    }

    [Fact]
    public void Run_RefusesANameThatCannotBeALabel() =>
        Check(("MacMini", "beszel.agent", Beszel())).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("cannot name an agent");

    [Fact]
    public void Run_RefusesAnAgentWithNoProgram() =>
        Check(("MacMini", "beszel-agent", new AgentSettings())).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("'program'");

    [Fact]
    public void Run_NamesTheNodeOfEveryProblem() =>
        Check(("MacMini", "beszel-agent", new AgentSettings()), ("wolfe-pi5", "beszel-agent", new AgentSettings()))
            .Errors.ShouldNotBeNull().Select(e => e.Message.Split(':')[0]).ShouldBe(["MacMini", "wolfe-pi5"]);

    [Fact]
    public void Run_RefusesAHomePathInTheEnvironment()
    {
        var agent = Beszel() with { Environment = new Dictionary<string, string> { ["DATA"] = "~/.local/share/thing" } };

        Check(("MacStudio", "beszel-agent", agent)).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("DATA");
    }
}
