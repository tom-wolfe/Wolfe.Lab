using System.Xml.Linq;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Agents;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Tests.Agents;

public class LaunchdSupervisorTests : IDisposable
{
    private readonly DirectoryInfo _agents = Directory.CreateTempSubdirectory("lab-agents-");
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    public void Dispose() => _agents.Delete(recursive: true);

    private LaunchdSupervisor Supervisor() =>
        new(new AgentDirectory(new PhysicalDirectory(_agents.FullName)), _commands, Substitute.For<IWorkflowLog>());

    private static AgentDefinition Agent(
        IReadOnlyDictionary<string, string>? environment = null,
        bool keepAlive = true,
        AgentRestart restart = AgentRestart.Reload) =>
        new(
            AgentLabel.ForName("ollama").ShouldNotBeNull(),
            HostPath.From("/opt/homebrew/bin/ollama"),
            ["serve"],
            environment ?? new Dictionary<string, string>(),
            null,
            HostPath.From("/tmp/ollama.log"),
            keepAlive,
            null,
            restart,
            DateTimeOffset.UnixEpoch);

    /// <summary>The value of a plist dict's key, as launchd would read it.</summary>
    private static string? Entry(string plist, string key)
    {
        var dictionary = XDocument.Parse(plist).Root.ShouldNotBeNull().Element("dict").ShouldNotBeNull();
        var keys = dictionary.Elements().ToList();
        var index = keys.FindIndex(e => e.Name == "key" && e.Value == key);
        return index < 0 ? null : keys[index + 1].Name == "true" ? "true" : keys[index + 1].Value;
    }

    [Fact]
    public void Render_NamesTheUnitAfterTheLabel() =>
        Supervisor().Render(Agent()).FileName.ShouldBe("dev.twolfe.ollama.plist");

    [Fact]
    public void Render_IsAPlistLaunchdCanRead()
    {
        var unit = Supervisor().Render(Agent());

        Entry(unit.Content, "Label").ShouldBe("dev.twolfe.ollama");
        Entry(unit.Content, "RunAtLoad").ShouldBe("true");
        Entry(unit.Content, "KeepAlive").ShouldBe("true");
        Entry(unit.Content, "StandardOutPath").ShouldBe("/tmp/ollama.log");
        Entry(unit.Content, "StandardErrorPath").ShouldBe("/tmp/ollama.log");
    }

    [Fact]
    public void Render_PutsTheProgramAtTheHeadOfTheArguments()
    {
        var unit = Supervisor().Render(Agent());
        var arguments = XDocument.Parse(unit.Content).Descendants("array").Single().Elements().Select(e => e.Value);

        arguments.ShouldBe(["/opt/homebrew/bin/ollama", "serve"]);
    }

    [Fact]
    public void Render_LeavesOutWhatWasNotDeclared() =>
        Entry(Supervisor().Render(Agent()).Content, "EnvironmentVariables").ShouldBeNull();

    [Fact]
    public void Render_OrdersTheEnvironmentSoADictionaryDoesNotReadAsAChange()
    {
        var supervisor = Supervisor();

        var one = supervisor.Render(Agent(new Dictionary<string, string> { ["OLLAMA_HOST"] = "0.0.0.0:11434", ["OLLAMA_MODELS"] = "/models" }));
        var other = supervisor.Render(Agent(new Dictionary<string, string> { ["OLLAMA_MODELS"] = "/models", ["OLLAMA_HOST"] = "0.0.0.0:11434" }));

        one.Content.ShouldBe(other.Content);
    }

    [Fact]
    public void Render_CarriesTheProgramsTimestampSoAnUpgradeCountsAsAChange()
    {
        var supervisor = Supervisor();
        var upgraded = Agent() with { ProgramStamp = DateTimeOffset.UnixEpoch.AddDays(1) };

        supervisor.Render(Agent()).Content.ShouldNotBe(supervisor.Render(upgraded).Content);
    }

    [Fact]
    public void Render_EscapesAValueThatWouldOtherwiseCloseATag()
    {
        var unit = Supervisor().Render(Agent(new Dictionary<string, string> { ["ARGS"] = "a & b <c>" }));

        unit.Content.ShouldContain("a &amp; b &lt;c&gt;");
        Entry(unit.Content, "EnvironmentVariables").ShouldNotBeNull();
        XDocument.Parse(unit.Content).Descendants("dict").Last().Elements("string").Single().Value.ShouldBe("a & b <c>");
    }

    [Fact]
    public void Render_SaysWhereTheUnitCameFrom() =>
        Supervisor().Render(Agent()).Content.ShouldContain("edits here are overwritten");

    [Fact]
    public async Task Converge_InstallsAnAgentLaunchdHasNeverHeardOf()
    {
        Loaded(false);

        var outcome = await Supervisor().Converge(Agent(), TestContext.Current.CancellationToken);

        outcome.ShouldBe(AgentOutcome.Installed);
        File.Exists(Path.Combine(_agents.FullName, "dev.twolfe.ollama.plist")).ShouldBeTrue();
        await Ran("bootstrap");
    }

    [Fact]
    public async Task Converge_DoesNothingWhenTheNodeAlreadySaysThis()
    {
        var supervisor = Supervisor();
        Install(supervisor.Render(Agent()).Content);
        Loaded(true);

        var outcome = await supervisor.Converge(Agent(), TestContext.Current.CancellationToken);

        outcome.ShouldBe(AgentOutcome.Unchanged);
        await NeverRan("bootout");
        await NeverRan("bootstrap");
    }

    [Fact]
    public async Task Converge_ReloadsARunningAgentWhoseUnitChanged()
    {
        var supervisor = Supervisor();
        Install(supervisor.Render(Agent(keepAlive: false)).Content);
        Loaded(true);

        var outcome = await supervisor.Converge(Agent(), TestContext.Current.CancellationToken);

        outcome.ShouldBe(AgentOutcome.Restarted);
        await Ran("bootout");
        await Ran("bootstrap");
    }

    [Fact]
    public async Task Converge_SignalsInsteadOfReloadingWhenTheAgentIsHostingThisJob()
    {
        var supervisor = Supervisor();
        Install(supervisor.Render(Agent(keepAlive: false, restart: AgentRestart.Signal)).Content);
        Loaded(true);

        var outcome = await supervisor.Converge(Agent(restart: AgentRestart.Signal), TestContext.Current.CancellationToken);

        outcome.ShouldBe(AgentOutcome.Restarted);
        await Ran("kill");
        await NeverRan("bootout");
    }

    [Fact]
    public async Task Converge_RewritesAUnitThatDriftedUnderALoadedAgent()
    {
        var supervisor = Supervisor();
        Install("<plist>someone edited this by hand</plist>");
        Loaded(true);

        await supervisor.Converge(Agent(), TestContext.Current.CancellationToken);

        File.ReadAllText(Path.Combine(_agents.FullName, "dev.twolfe.ollama.plist"))
            .ShouldBe(supervisor.Render(Agent()).Content);
    }

    private void Install(string content) =>
        File.WriteAllText(Path.Combine(_agents.FullName, "dev.twolfe.ollama.plist"), content);

    /// <summary>`launchctl list` says what launchd is running; `id -u` answers everything else.</summary>
    private void Loaded(bool loaded) =>
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(call =>
            call.Arg<Command>().Arguments.Contains("list")
                ? new CommandResult(0, loaded ? "PID\tStatus\tLabel\n-\t0\tdev.twolfe.ollama\n" : "PID\tStatus\tLabel\n", "")
                : new CommandResult(0, "501", ""));

    private async Task Ran(string verb) =>
        await _commands.Received().Run(Arg.Is<Command>(c => c.Arguments.Contains(verb)), Arg.Any<CancellationToken>());

    private async Task NeverRan(string verb) =>
        await _commands.DidNotReceive().Run(Arg.Is<Command>(c => c.Arguments.Contains(verb)), Arg.Any<CancellationToken>());
}
