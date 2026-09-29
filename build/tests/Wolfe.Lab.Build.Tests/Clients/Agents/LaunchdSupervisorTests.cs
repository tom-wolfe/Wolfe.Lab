using System.Xml.Linq;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Clients.Agents.Launchd;
using Wolfe.Lab.Build.Tests.Clients.Resilience;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Tests.Clients.Agents;

public class LaunchdSupervisorTests : IDisposable
{
    private readonly DirectoryInfo _agents = Directory.CreateTempSubdirectory("lab-agents-");
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    public void Dispose() => _agents.Delete(recursive: true);

    private LaunchdSupervisor Supervisor() =>
        new(new AgentDirectory(new PhysicalDirectory(_agents.FullName)), _commands, Substitute.For<IWorkflowLog>(),
            Pipelines.Polling(LaunchdSupervisor.Unloading, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(1)),
            Microsoft.Extensions.Options.Options.Create(new LaunchdOptions { Margin = TimeSpan.FromSeconds(1) }));

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
    public async Task Converge_LoadsTheNewUnitOnlyOnceLaunchdHasLetGoOfTheOld()
    {
        // Alloy, flushing its queues: still loaded for a few listings after its bootout.
        var supervisor = Supervisor();
        Install(supervisor.Render(Agent(keepAlive: false)).Content);
        Loaded(true, lingers: 3);

        await supervisor.Converge(Agent(), TestContext.Current.CancellationToken);

        var verbs = _commands.ReceivedCalls()
            .Select(call => ((Command)call.GetArguments()[0]!).Arguments.FirstOrDefault())
            .Where(verb => verb is "bootout" or "list" or "bootstrap")
            .ToList();
        verbs.SkipWhile(verb => verb != "bootout").ShouldBe(["bootout", "list", "list", "list", "list", "bootstrap"]);
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

    /// <summary>
    /// `launchctl list` says what launchd is running — until a `bootout`, after which the unit
    /// lingers for as many listings as it takes to stop; `id -u` answers everything else.
    /// </summary>
    private void Loaded(bool loaded, int lingers = 0)
    {
        var stopping = false;
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var arguments = call.Arg<Command>().Arguments;
            if (arguments.Contains("bootout"))
            {
                stopping = true;
            }
            else if (arguments.Contains("bootstrap"))
            {
                (stopping, loaded) = (false, true);
            }
            else if (arguments.Contains("list"))
            {
                if (stopping && lingers-- <= 0)
                {
                    (stopping, loaded) = (false, false);
                }

                return new CommandResult(0, loaded ? "PID\tStatus\tLabel\n-\t0\tdev.twolfe.ollama\n" : "PID\tStatus\tLabel\n", "");
            }

            return new CommandResult(0, "501", "");
        });
    }

    private async Task Ran(string verb) =>
        await _commands.Received().Run(Arg.Is<Command>(c => c.Arguments.Contains(verb)), Arg.Any<CancellationToken>());

    private async Task NeverRan(string verb) =>
        await _commands.DidNotReceive().Run(Arg.Is<Command>(c => c.Arguments.Contains(verb)), Arg.Any<CancellationToken>());

    [Fact]
    public async Task Converge_WritesTheUnitForTheOwnerAlone()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix permissions.");
        Loaded(false);

        await Supervisor().Converge(Agent(), TestContext.Current.CancellationToken);

        new PhysicalFile(Path.Combine(_agents.FullName, "dev.twolfe.ollama.plist")).GetUnixFileMode()
            .ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Fact]
    public async Task Retire_UnloadsAndRemovesAUnitSoItDoesNotComeBackAtLogin()
    {
        File.WriteAllText(Path.Combine(_agents.FullName, "sh.brew.beszel-agent.plist"), "<plist/>");
        var loaded = true;
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var arguments = call.Arg<Command>().Arguments;
            if (arguments.Contains("bootout"))
            {
                loaded = false;
            }

            return arguments.Contains("list")
                ? new CommandResult(0, loaded ? "PID\tStatus\tLabel\n-\t0\tsh.brew.beszel-agent\n" : "PID\tStatus\tLabel\n", "")
                : new CommandResult(0, "501", "");
        });

        await Supervisor().Retire("sh.brew.beszel-agent", TestContext.Current.CancellationToken);

        await _commands.Received().Run(
            Arg.Is<Command>(c => c.Arguments.SequenceEqual(new[] { "bootout", "gui/501/sh.brew.beszel-agent" })),
            Arg.Any<CancellationToken>());
        File.Exists(Path.Combine(_agents.FullName, "sh.brew.beszel-agent.plist")).ShouldBeFalse();
    }

    [Fact]
    public async Task Retire_DoesNothingToAUnitThatIsNotThere()
    {
        Loaded(false);

        await Supervisor().Retire("sh.brew.beszel-agent", TestContext.Current.CancellationToken);

        await NeverRan("bootout");
    }

    [Fact]
    public async Task IsInstalled_CountsAUnitOnDiskThatIsNotLoaded()
    {
        File.WriteAllText(Path.Combine(_agents.FullName, "sh.brew.beszel-agent.plist"), "<plist/>");
        Loaded(false);

        (await Supervisor().IsInstalled("sh.brew.beszel-agent", TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await Supervisor().IsInstalled("sh.brew.other", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public void Render_StampsTheUnitWithItsArtifactsSoAChangedConfigRestartsIt()
    {
        var unit = Supervisor().Render(Agent() with { ArtifactStamp = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero) });

        unit.Content.ShouldContain(" artifacts: 2026-09-29T12:00:00.0000000Z");
    }

    [Fact]
    public void Render_IsUnchangedForAnAgentWithoutArtifacts()
    {
        // A unit is compared as text: an extra line for agents that publish nothing would
        // restart every one of them on the deploy that shipped this.
        Supervisor().Render(Agent()).Content.ShouldNotContain("artifacts:");
    }
}
