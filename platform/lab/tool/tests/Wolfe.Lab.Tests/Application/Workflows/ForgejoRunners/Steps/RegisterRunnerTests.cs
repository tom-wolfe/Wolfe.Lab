using Ritten.Docker;
using Wolfe.Lab.Application.Workflows.ForgejoRunners.Models;
using Wolfe.Lab.Application.Workflows.ForgejoRunners.Steps;
using Wolfe.Lab.Domain.Secrets;

namespace Wolfe.Lab.Tests.Application.Workflows.ForgejoRunners.Steps;

public class RegisterRunnerTests
{
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();

    private RegisterRunner Step(bool dryRun = false) =>
        new(_docker, _secrets, new WorkflowJob("forgejo", "register-runner", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>());

    private const string Secret = "0123456789abcdef0123456789abcdef01234567";

    public RegisterRunnerTests() =>
        _docker.Inspect("forgejo", Arg.Any<CancellationToken>()).Returns(new ContainerState("codeberg.org/forgejo/forgejo:13", Running: true));

    private ContainerExec Registration() =>
        _docker.ReceivedCalls().Select(call => call.GetArguments()[0]).OfType<ContainerExec>().Single();

    private static RunnerRegistration Host => new("MacMini", "MacMini:host", "tom-wolfe/Wolfe.Lab", SecretReference.From("op://Wolfe.Lab/forgejo-runner-MacMini/credential"));

    [Fact]
    public async Task Run_RegistersAsGitWithTheSecretOnStdin()
    {
        _secrets.Resolve(Host.Secret.Value, Arg.Any<CancellationToken>()).Returns(Secret);

        var result = await Step().Run(Host, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        var exec = Registration();
        exec.Container.ShouldBe("forgejo");
        exec.User.ShouldBe("git");
        exec.Arguments.ShouldBe(["forgejo", "forgejo-cli", "actions", "register", "--secret-stdin", "--name", "MacMini", "--labels", "MacMini:host", "--scope", "tom-wolfe/Wolfe.Lab"]);
        exec.Input.ShouldBe(Secret);
        exec.Arguments.ShouldNotContain(Secret);
        exec.IsReadOnly.ShouldBeFalse();
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

        _docker.ReceivedCalls().ShouldBeEmpty();
        _secrets.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_RefusesWhereThereIsNoForgejo()
    {
        _docker.Inspect("forgejo", Arg.Any<CancellationToken>()).Returns((ContainerState?)null);

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
        await _docker.DidNotReceive().Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>());
    }
}
