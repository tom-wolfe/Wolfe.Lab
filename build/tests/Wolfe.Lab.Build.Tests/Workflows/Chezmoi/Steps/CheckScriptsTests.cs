using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Workflows.Chezmoi.Models;
using Wolfe.Lab.Build.Workflows.Chezmoi.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Chezmoi.Steps;

public class CheckScriptsTests
{
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    private CheckScripts Step() => new(_commands, Substitute.For<IWorkflowLog>());

    private static RenderedProfiles Rendered(params (string Profile, string[] Files)[] profiles) =>
        new([.. profiles.Select(p => new RenderedProfile(p.Profile, new PhysicalDirectory($"/render/{p.Profile}"), p.Files))]);

    [Fact]
    public async Task Run_ShellchecksEveryRenderedScriptAcrossProfiles()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));
        var rendered = Rendered(
            ("macbook", [".chezmoiscripts/install-packages.sh", ".zshrc"]),
            ("pi-node", [".chezmoiscripts/install-packages.sh", ".chezmoiscripts/run_once_dotnet.sh"]));

        var result = await Step().Run(rendered, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        var command = (Command)_commands.ReceivedCalls().Single().GetArguments()[0]!;
        command.Path.ShouldBe("shellcheck");
        command.Arguments.ShouldBe([
            "/render/macbook/.chezmoiscripts/install-packages.sh",
            "/render/pi-node/.chezmoiscripts/install-packages.sh",
            "/render/pi-node/.chezmoiscripts/run_once_dotnet.sh"
        ]);
    }

    [Fact]
    public async Task Run_FailsWithShellchecksOwnWordsWhenAScriptIsWrong()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(1, "SC2086: Double quote to prevent globbing.", ""));

        var result = await Step().Run(Rendered(("macbook", [".chezmoiscripts/x.sh"])), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("SC2086");
    }

    [Fact]
    public async Task Run_RunsNothingWhenNoScriptRendered()
    {
        var result = await Step().Run(Rendered(("macbook", [".zshrc"])), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _commands.ReceivedCalls().ShouldBeEmpty();
    }
}
