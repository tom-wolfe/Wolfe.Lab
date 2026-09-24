using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Clients.Agents.Systemd;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Tests.Clients.Agents;

public class SystemdSupervisorTests : IDisposable
{
    private const string UnitName = "dev.twolfe.beszel-agent.service";

    private readonly DirectoryInfo _units = Directory.CreateTempSubdirectory("lab-units-");
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    public void Dispose() => _units.Delete(recursive: true);

    private SystemdSupervisor Supervisor() =>
        new(new AgentDirectory(new PhysicalDirectory(_units.FullName)), _commands, Substitute.For<IWorkflowLog>());

    private static AgentDefinition Agent(
        IReadOnlyDictionary<string, string>? environment = null,
        IReadOnlyList<string>? arguments = null,
        bool keepAlive = true,
        AgentRestart restart = AgentRestart.Reload) =>
        new(
            AgentLabel.ForName("beszel-agent").ShouldNotBeNull(),
            HostPath.From("/home/tomwolfe/.local/bin/beszel-agent"),
            arguments ?? [],
            environment ?? new Dictionary<string, string>(),
            null,
            HostPath.From("/home/tomwolfe/.cache/beszel/beszel-agent.log"),
            keepAlive,
            30,
            restart,
            DateTimeOffset.UnixEpoch);

    /// <summary>The lines of a unit that set <paramref name="key"/>, in order.</summary>
    private static string[] Settings(string unit, string key) =>
        [.. unit.Split('\n').Where(line => line.StartsWith(key + "=", StringComparison.Ordinal)).Select(line => line[(key.Length + 1)..])];

    [Fact]
    public void Render_NamesTheUnitAfterTheLabel() =>
        Supervisor().Render(Agent()).FileName.ShouldBe(UnitName);

    [Fact]
    public void Render_IsAServiceSystemdWillKeepRunning()
    {
        var unit = Supervisor().Render(Agent()).Content;

        unit.ShouldContain("[Service]");
        Settings(unit, "ExecStart").ShouldBe(["\"/home/tomwolfe/.local/bin/beszel-agent\""]);
        Settings(unit, "Restart").ShouldBe(["always"]);
        Settings(unit, "StartLimitIntervalSec").ShouldBe(["0"]);
        Settings(unit, "TimeoutStopSec").ShouldBe(["30"]);
        Settings(unit, "StandardOutput").ShouldBe(["append:/home/tomwolfe/.cache/beszel/beszel-agent.log"]);
        Settings(unit, "StandardError").ShouldBe(["append:/home/tomwolfe/.cache/beszel/beszel-agent.log"]);
        Settings(unit, "WantedBy").ShouldBe(["default.target"]);
    }

    [Fact]
    public void Render_DoesNotRestartWhatShouldNotBeKeptAlive() =>
        Settings(Supervisor().Render(Agent(keepAlive: false)).Content, "Restart").ShouldBe(["no"]);

    [Fact]
    public void Render_QuotesEachArgumentAsOneWord() =>
        Settings(Supervisor().Render(Agent(arguments: ["serve", "--name", "two words"])).Content, "ExecStart")
            .ShouldBe(["\"/home/tomwolfe/.local/bin/beszel-agent\" \"serve\" \"--name\" \"two words\""]);

    [Fact]
    public void Render_KeepsWhatSystemdWouldOtherwiseExpandLiteral()
    {
        // $ is expanded in ExecStart, % in every setting, and a quote or backslash ends or
        // escapes the word: each must reach the process as written.
        var unit = Supervisor().Render(Agent(
            environment: new Dictionary<string, string> { ["TOKEN"] = "50%\"off\"\\" },
            arguments: ["$HOME", "100%"])).Content;

        Settings(unit, "ExecStart").ShouldBe(["\"/home/tomwolfe/.local/bin/beszel-agent\" \"$$HOME\" \"100%%\""]);
        Settings(unit, "Environment").ShouldBe(["\"TOKEN=50%%\\\"off\\\"\\\\\""]);
    }

    [Fact]
    public void Render_OrdersTheEnvironmentSoADictionaryDoesNotReadAsAChange()
    {
        var supervisor = Supervisor();

        var one = supervisor.Render(Agent(new Dictionary<string, string> { ["HUB_URL"] = "http://hub", ["KEY"] = "k" }));
        var other = supervisor.Render(Agent(new Dictionary<string, string> { ["KEY"] = "k", ["HUB_URL"] = "http://hub" }));

        one.Content.ShouldBe(other.Content);
        Settings(one.Content, "Environment").ShouldBe(["\"HUB_URL=http://hub\"", "\"KEY=k\""]);
    }

    [Fact]
    public void Render_CarriesTheProgramsTimestampSoAnUpgradeCountsAsAChange()
    {
        var supervisor = Supervisor();
        var upgraded = Agent() with { ProgramStamp = DateTimeOffset.UnixEpoch.AddDays(1) };

        supervisor.Render(Agent()).Content.ShouldNotBe(supervisor.Render(upgraded).Content);
    }

    [Fact]
    public void Render_SaysWhereTheUnitCameFrom() =>
        Supervisor().Render(Agent()).Content.ShouldContain("edits here are overwritten");

    [Fact]
    public async Task Converge_EnablesAndStartsAnAgentSystemdIsNotRunning()
    {
        Running(false);

        var outcome = await Supervisor().Converge(Agent(), TestContext.Current.CancellationToken);

        outcome.ShouldBe(AgentOutcome.Installed);
        File.Exists(Path.Combine(_units.FullName, UnitName)).ShouldBeTrue();
        await Ran("daemon-reload");
        await Ran("enable");
        await Ran("restart");
    }

    [Fact]
    public async Task Converge_DoesNothingWhenTheNodeAlreadySaysThis()
    {
        var supervisor = Supervisor();
        Install(supervisor.Render(Agent()).Content);
        Running(true);

        var outcome = await supervisor.Converge(Agent(), TestContext.Current.CancellationToken);

        outcome.ShouldBe(AgentOutcome.Unchanged);
        await NeverRan("daemon-reload");
        await NeverRan("restart");
    }

    [Fact]
    public async Task Converge_ReloadsTheManagerAndRestartsARunningAgentWhoseUnitChanged()
    {
        var supervisor = Supervisor();
        Install(supervisor.Render(Agent(keepAlive: false)).Content);
        Running(true);

        var outcome = await supervisor.Converge(Agent(), TestContext.Current.CancellationToken);

        outcome.ShouldBe(AgentOutcome.Restarted);
        Received.InOrder(() =>
        {
            _commands.Run(Arg.Is<Command>(c => c.Arguments.Contains("daemon-reload")), Arg.Any<CancellationToken>());
            _commands.Run(Arg.Is<Command>(c => c.Arguments.Contains("restart")), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Converge_SignalsOnlyTheMainProcessWhenTheAgentIsHostingThisJob()
    {
        var supervisor = Supervisor();
        Install(supervisor.Render(Agent(keepAlive: false, restart: AgentRestart.Signal)).Content);
        Running(true);

        var outcome = await supervisor.Converge(Agent(restart: AgentRestart.Signal), TestContext.Current.CancellationToken);

        outcome.ShouldBe(AgentOutcome.Restarted);
        await Ran("daemon-reload");
        await _commands.Received().Run(
            Arg.Is<Command>(c => c.Path == "kill" && c.Arguments.SequenceEqual(new[] { "-TERM", "4242" })),
            Arg.Any<CancellationToken>());
        await NeverRan("restart");
    }

    [Fact]
    public async Task Converge_ReinstallsAnAgentThatIsInstalledButNotEnabled()
    {
        var supervisor = Supervisor();
        Install(supervisor.Render(Agent()).Content);
        Running(true, enabled: false);

        var outcome = await supervisor.Converge(Agent(), TestContext.Current.CancellationToken);

        outcome.ShouldBe(AgentOutcome.Installed);
        await Ran("enable");
    }

    [Fact]
    public async Task Converge_RewritesAUnitThatDriftedUnderARunningAgent()
    {
        var supervisor = Supervisor();
        Install("[Service]\nExecStart=someone edited this by hand\n");
        Running(true);

        await supervisor.Converge(Agent(), TestContext.Current.CancellationToken);

        File.ReadAllText(Path.Combine(_units.FullName, UnitName)).ShouldBe(supervisor.Render(Agent()).Content);
    }

    [Theory]
    [InlineData("plain", "\"plain\"")]
    [InlineData("a\tb", "\"a\\tb\"")]
    [InlineData("line\nbreak", "\"line\\nbreak\"")]
    public void Quoted_EscapesWhatAUnitLineCannotHold(string value, string expected) =>
        SystemdSupervisor.Quoted(value).ShouldBe(expected);

    private void Install(string content) =>
        File.WriteAllText(Path.Combine(_units.FullName, UnitName), content);

    /// <summary>`systemctl show` answers the state and the main pid; everything else succeeds.</summary>
    private void Running(bool active, bool enabled = true) =>
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var arguments = call.Arg<Command>().Arguments;
            if (arguments.Contains("--property=MainPID"))
            {
                return new CommandResult(0, "4242\n", "");
            }

            return arguments.Contains("show")
                ? new CommandResult(0, $"ActiveState={(active ? "active" : "inactive")}\nUnitFileState={(enabled ? "enabled" : "disabled")}\n", "")
                : new CommandResult(0, "", "");
        });

    private async Task Ran(string verb) =>
        await _commands.Received().Run(Arg.Is<Command>(c => c.Arguments.Contains(verb)), Arg.Any<CancellationToken>());

    private async Task NeverRan(string verb) =>
        await _commands.DidNotReceive().Run(Arg.Is<Command>(c => c.Arguments.Contains(verb)), Arg.Any<CancellationToken>());

    [Fact]
    public async Task Converge_WritesTheUnitForTheOwnerAlone()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix permissions.");
        Running(false);

        await Supervisor().Converge(Agent(), TestContext.Current.CancellationToken);

        new PhysicalFile(Path.Combine(_units.FullName, UnitName)).GetUnixFileMode().ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Fact]
    public async Task Retire_DisablesStopsAndRemovesTheUnit()
    {
        File.WriteAllText(Path.Combine(_units.FullName, "beszel-agent.service"), "[Service]\n");
        Running(true);

        await Supervisor().Retire("beszel-agent", TestContext.Current.CancellationToken);

        await _commands.Received().Run(
            Arg.Is<Command>(c => c.Arguments.SequenceEqual(new[] { "--user", "disable", "--now", "beszel-agent.service" })),
            Arg.Any<CancellationToken>());
        File.Exists(Path.Combine(_units.FullName, "beszel-agent.service")).ShouldBeFalse();
        await Ran("daemon-reload");
    }

    [Theory]
    [InlineData("beszel-agent", "beszel-agent.service")]
    [InlineData("beszel-agent.service", "beszel-agent.service")]
    [InlineData("beszel-agent.path", "beszel-agent.path")]
    public void UnitFileName_IsAServiceUnlessItNamesItsType(string unit, string expected) =>
        SystemdSupervisor.UnitFileName(unit).ShouldBe(expected);

    [Fact]
    public async Task IsInstalled_AsksSystemdWhenThereIsNoFile()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "not-found\n", ""));

        (await Supervisor().IsInstalled("beszel-agent", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
