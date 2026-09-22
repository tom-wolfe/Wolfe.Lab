using System.Formats.Tar;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Chezmoi;

namespace Wolfe.Lab.Build.Tests.Clients.Chezmoi;

public class ChezmoiClientTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-chezmoi-test-");
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    public void Dispose() => _root.Delete(recursive: true);

    [Fact]
    public async Task Render_ArchivesTheSourceForTheProfileWithTheVaultStubbedAndExtractsIt()
    {
        // The fake chezmoi writes what a real one would — an archive at the --output it was
        // given — and the fake tar extracts it.
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var command = call.Arg<Command>();
            if (command.Path == "tar")
            {
                TarFile.ExtractToDirectory(command.Arguments[^1], command.Arguments[Array.IndexOf(command.Arguments, "-C") + 1], overwriteFiles: true);
                return new CommandResult(0, "", "");
            }

            if (command.Path != "chezmoi")
            {
                return new CommandResult(0, "", "");
            }

            // Entry by entry: TarFile.CreateFromDirectory skips dotfiles as hidden, and a home
            // tree is nothing else.
            var output = command.Arguments[Array.IndexOf(command.Arguments, "--output") + 1];
            var script = Path.Combine(_root.FullName, "install-packages.sh");
            File.WriteAllText(script, "#!/bin/sh\n");
            using (var writer = new TarWriter(File.Create(output)))
            {
                writer.WriteEntry(script, ".chezmoiscripts/install-packages.sh");
                writer.WriteEntry(script, ".zshrc");
            }

            return new CommandResult(0, "", "");
        });
        var source = new PhysicalDirectory(Path.Combine(_root.FullName, "checkout"));
        var destination = new PhysicalDirectory(Path.Combine(_root.FullName, "render", "pi-node"));

        var files = await new ChezmoiClient(_commands).Render(source, "pi-node", destination, TestContext.Current.CancellationToken);

        files.ShouldBe([".chezmoiscripts/install-packages.sh", ".zshrc"]);
        File.Exists(Path.Combine(destination.AbsolutePath, ".zshrc")).ShouldBeTrue();

        var command = (Command)_commands.ReceivedCalls().First().GetArguments()[0]!;
        command.Path.ShouldBe("chezmoi");
        command.Arguments.ShouldContain("archive");
        command.Arguments.ShouldContain("--exclude");
        command.Arguments.ShouldContain("externals");
        command.Arguments[Array.IndexOf(command.Arguments, "--source") + 1].ShouldBe(source.AbsolutePath);
        command.Arguments[Array.IndexOf(command.Arguments, "--destination") + 1].ShouldBe(destination.AbsolutePath);
        command.EnvironmentVariables[ChezmoiClient.TokenVariable].ShouldBe(ChezmoiClient.StubToken);
    }

    [Fact]
    public async Task Render_WritesAConfigThatSuppliesTheProfileAndStubsOp()
    {
        string? config = null;
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var command = call.Arg<Command>();
            if (command.Path != "chezmoi")
            {
                return new CommandResult(0, "", "");
            }

            config = File.ReadAllText(command.Arguments[Array.IndexOf(command.Arguments, "--config") + 1]);
            var stub = command.Arguments[Array.IndexOf(command.Arguments, "--config") + 1].Replace("chezmoi.toml", "op");
            File.Exists(stub).ShouldBeTrue();
            TarFile.CreateFromDirectory(Directory.CreateDirectory(Path.Combine(_root.FullName, "empty")).FullName, command.Arguments[Array.IndexOf(command.Arguments, "--output") + 1], includeBaseDirectory: false);
            return new CommandResult(0, "", "");
        });

        await new ChezmoiClient(_commands).Render(new PhysicalDirectory(_root.FullName), "macbook", new PhysicalDirectory(Path.Combine(_root.FullName, "out")), TestContext.Current.CancellationToken);

        config.ShouldNotBeNull();
        config.ShouldContain("profile = \"macbook\"");
        config.ShouldContain("mode = \"service\"");
        config.ShouldContain("command = \"");
    }

    [Fact]
    public async Task Render_RemovesItsScratchWhateverItHolds()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var command = call.Arg<Command>();
            if (command.Path == "chezmoi")
            {
                TarFile.CreateFromDirectory(Directory.CreateDirectory(Path.Combine(_root.FullName, "empty")).FullName, command.Arguments[Array.IndexOf(command.Arguments, "--output") + 1], includeBaseDirectory: false);
            }

            return new CommandResult(0, "", "");
        });

        await new ChezmoiClient(_commands).Render(new PhysicalDirectory(_root.FullName), "macbook", new PhysicalDirectory(Path.Combine(_root.FullName, "out")), TestContext.Current.CancellationToken);

        var rm = _commands.ReceivedCalls().Select(c => (Command)c.GetArguments()[0]!).Single(c => c.Path == "rm");
        rm.Arguments[0].ShouldBe("-rf");
        rm.Arguments[1].ShouldContain("lab-chezmoi-");
    }

    [Fact]
    public async Task Update_PullsAndAppliesWithoutATerminal()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));

        await new ChezmoiClient(_commands).Update(TestContext.Current.CancellationToken);

        var command = (Command)_commands.ReceivedCalls().Single().GetArguments()[0]!;
        command.Arguments.ShouldBe(["update", "--init", "--no-tty"]);
        command.ThrowsOnError.ShouldBeTrue();
    }
}
