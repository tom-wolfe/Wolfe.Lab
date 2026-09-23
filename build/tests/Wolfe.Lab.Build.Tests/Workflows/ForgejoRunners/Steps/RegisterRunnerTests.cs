using Wolfe.Lab.Build.Clients.Secrets;
using Wolfe.Lab.Build.Workflows.ForgejoRunners.Models;
using Wolfe.Lab.Build.Workflows.ForgejoRunners.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.ForgejoRunners.Steps;

public class RegisterRunnerTests
{
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();

    private RegisterRunner Step(bool dryRun = false) =>
        new(_commands, _secrets, new WorkflowJob("forgejo", "register-runner", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>());

    private static RunnerRegistration Host => new("MacMini", "MacMini:host", "tom-wolfe/Wolfe.Lab", SecretReference.From("op://Wolfe.Lab/forgejo-runner-MacMini/credential"));

    [Fact]
    public async Task Run_RegistersAsGitWithTheSecretOnStdin()
    {
        _secrets.Resolve(Host.Secret.Value, Arg.Any<CancellationToken>()).Returns("0123456789abcdef0123456789abcdef01234567");
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));

        var result = await Step().Run(Host, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        var command = (Command)_commands.ReceivedCalls().Single().GetArguments()[0]!;
        command.Path.ShouldBe("docker");
        command.Arguments.ShouldBe(["exec", "-i", "-u", "git", "forgejo", "forgejo", "forgejo-cli", "actions", "register", "--secret-stdin", "--name", "MacMini", "--labels", "MacMini:host", "--scope", "tom-wolfe/Wolfe.Lab"]);
        command.StandardInput.ShouldBe("0123456789abcdef0123456789abcdef01234567");
        command.Arguments.ShouldNotContain(command.StandardInput);
    }

    [Fact]
    public async Task Run_AnInstanceRunnerHasNoScope()
    {
        _secrets.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("s");
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));

        await Step().Run(Host with { Scope = null }, TestContext.Current.CancellationToken);

        ((Command)_commands.ReceivedCalls().Single().GetArguments()[0]!).Arguments.ShouldNotContain("--scope");
    }

    [Fact]
    public async Task Run_RehearsalReadsNothingAndRegistersNothing()
    {
        await Step(dryRun: true).Run(Host, TestContext.Current.CancellationToken);

        _commands.ReceivedCalls().ShouldBeEmpty();
        _secrets.ReceivedCalls().ShouldBeEmpty();
    }
}
