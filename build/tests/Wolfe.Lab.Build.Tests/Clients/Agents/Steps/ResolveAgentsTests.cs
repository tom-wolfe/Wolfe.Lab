using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Clients.Agents.Steps;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Tests.Clients.Agents.Steps;

public class ResolveAgentsTests : IDisposable
{
    private readonly DirectoryInfo _node = Directory.CreateTempSubdirectory("lab-node-");

    public void Dispose() => _node.Delete(recursive: true);

    private string Program(string name = "ollama")
    {
        var path = Path.Combine(_node.FullName, name);
        File.WriteAllText(path, "");
        return path;
    }

    private static ResolveAgents Step(params (string Name, AgentSettings Settings)[] agents) =>
        new(new AgentDeclarations(agents.ToDictionary(a => a.Name, a => a.Settings)), Substitute.For<IWorkflowLog>());

    [Fact]
    public void Run_ResolvesWhatTheNodeCanActuallyRun()
    {
        var result = Step(("ollama", new AgentSettings { Program = HostPath.From(Program()), Arguments = ["serve"] })).Run();

        var agent = result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem();
        agent.Label.Value.ShouldBe("dev.twolfe.ollama");
        agent.Arguments.ShouldBe(["serve"]);
    }

    [Fact]
    public void Run_StampsTheAgentWithTheProgramItFound()
    {
        var program = Program();

        var result = Step(("ollama", new AgentSettings { Program = HostPath.From(program) })).Run();

        result.Value.ShouldNotBeNull().Agents.ShouldHaveSingleItem()
            .ProgramStamp.ShouldBe(File.GetLastWriteTimeUtc(program));
    }

    [Fact]
    public void Run_RefusesAnAgentWhoseProgramIsNotOnThisNode()
    {
        var result = Step(("ollama", new AgentSettings { Program = HostPath.From(Path.Combine(_node.FullName, "absent")) })).Run();

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("not on this node");
    }

    [Fact]
    public void Run_RefusesAnAgentThatNamesNoProgram()
    {
        var result = Step(("ollama", new AgentSettings())).Run();

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("'program'");
    }

    [Fact]
    public void Run_RefusesAKeyThatCannotBeALabel()
    {
        var result = Step(("not a name", new AgentSettings { Program = HostPath.From(Program()) })).Run();

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("cannot name an agent");
    }

    [Fact]
    public void Run_ReportsEverySlicesProblemAtOnce()
    {
        var result = Step(
            ("ollama", new AgentSettings()),
            ("watcher", new AgentSettings { Program = HostPath.From(Path.Combine(_node.FullName, "absent")) })).Run();

        result.Outcome.Errors.ShouldNotBeNull().Count.ShouldBe(2);
    }

    [Fact]
    public void Run_OrdersTheAgentsSoAConvergeIsRepeatable()
    {
        var result = Step(
            ("watcher", new AgentSettings { Program = HostPath.From(Program("watcher")) }),
            ("ollama", new AgentSettings { Program = HostPath.From(Program()) })).Run();

        result.Value.ShouldNotBeNull().Agents.Select(a => a.Label.Name).ShouldBe(["ollama", "watcher"]);
    }
}
