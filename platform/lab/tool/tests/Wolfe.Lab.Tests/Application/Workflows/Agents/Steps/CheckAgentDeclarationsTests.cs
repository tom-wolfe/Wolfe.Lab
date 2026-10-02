using Wolfe.Lab.Application.Workflows.Agents.Models;
using Wolfe.Lab.Application.Workflows.Agents.Steps;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;

namespace Wolfe.Lab.Tests.Application.Workflows.Agents.Steps;

public class CheckAgentDeclarationsTests
{
    private static StepResult Check(params (string Node, string Agent, AgentOptions Options)[] agents)
    {
        var nodes = agents
            .GroupBy(a => a.Node)
            .ToDictionary(g => g.Key, g => new NodeAgentsOptions { Agents = g.ToDictionary(a => a.Agent, a => a.Options) });
        return new CheckAgentDeclarations(new AgentsDeclaredPerNode(nodes), Substitute.For<IWorkflowLog>()).Run();
    }

    private static AgentOptions Beszel(string token = "op://Wolfe.Lab/beszel-agent/credential") => new()
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
        Check(("MacMini", "beszel-agent", new AgentOptions())).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("'program'");

    [Fact]
    public void Run_NamesTheNodeOfEveryProblem() =>
        Check(("MacMini", "beszel-agent", new AgentOptions()), ("wolfe-pi5", "beszel-agent", new AgentOptions()))
            .Errors.ShouldNotBeNull().Select(e => e.Message.Split(':')[0]).ShouldBe(["MacMini", "wolfe-pi5"]);

    [Fact]
    public void Run_RefusesAHomePathInTheEnvironment()
    {
        var agent = Beszel() with { Environment = new Dictionary<string, string> { ["DATA"] = "~/.local/share/thing" } };

        Check(("MacStudio", "beszel-agent", agent)).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("DATA");
    }
}
