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

    private const string Secret = "0123456789abcdef0123456789abcdef01234567";

    public RegisterRunnerTests() =>
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "", ""));

    private Command Registration() =>
        _commands.ReceivedCalls().Select(call => (Command)call.GetArguments()[0]!).Single(command => command.Arguments.Contains("register"));

    private static RunnerRegistration Host => new("MacMini", "MacMini:host", "tom-wolfe/Wolfe.Lab", SecretReference.From("op://Wolfe.Lab/forgejo-runner-MacMini/credential"));

    [Fact]
    public async Task Run_RegistersAsGitWithTheSecretOnStdin()
    {
        _secrets.Resolve(Host.Secret.Value, Arg.Any<CancellationToken>()).Returns(Secret);

        var result = await Step().Run(Host, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        var command = Registration();
        command.Path.ShouldBe("docker");
        command.Arguments.ShouldBe(["exec", "-i", "-u", "git", "forgejo", "forgejo", "forgejo-cli", "actions", "register", "--secret-stdin", "--name", "MacMini", "--labels", "MacMini:host", "--scope", "tom-wolfe/Wolfe.Lab"]);
        command.StandardInput.ShouldBe("0123456789abcdef0123456789abcdef01234567");
        command.Arguments.ShouldNotContain(command.StandardInput);
    }

    [Fact]
    public async Task Run_AnInstanceRunnerHasNoScope()
    {
        _secrets.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Secret);

        await Step().Run(Host with { Scope = null }, TestContext.Current.CancellationToken);

        Registration().Arguments.ShouldNotContain("--scope");
    }

    [Fact]
    public async Task Run_RehearsalReadsNothingAndRegistersNothing()
    {
        await Step(dryRun: true).Run(Host, TestContext.Current.CancellationToken);

        _commands.ReceivedCalls().ShouldBeEmpty();
        _secrets.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_RefusesWhereThereIsNoForgejo()
    {
        _commands.Run(Arg.Is<Command>(c => c.Arguments.Contains("inspect")), Arg.Any<CancellationToken>()).Returns(new CommandResult(1, "", "No such container"));

        var result = await Step().Run(Host, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("on the mini");
        _secrets.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("0123456789abcdef")]
    public async Task Run_RefusesASecretThatIsNotARunnerSecret(string secret)
    {
        _secrets.Resolve(Host.Secret.Value, Arg.Any<CancellationToken>()).Returns(secret);

        var result = await Step().Run(Host, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain($"holds {secret.Length} characters");
        _commands.ReceivedCalls().Select(call => (Command)call.GetArguments()[0]!).ShouldNotContain(c => c.Arguments.Contains("register"));
    }
}
