using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Tests.Secrets;

public class OnePasswordSecretsTests
{
    private static readonly SecretReference Reference = SecretReference.From("op://Wolfe.Lab/item/field");
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();

    [Fact]
    public async Task Read_ReturnsWhatOpPrints()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "s3cret", ""));

        var value = await new OnePasswordSecrets(_commands).Read(Reference, TestContext.Current.CancellationToken);

        value.ShouldBe("s3cret");
        var command = (Command)_commands.ReceivedCalls().Single().GetArguments()[0]!;
        command.Path.ShouldBe("op");
        command.Arguments.ShouldBe(["read", "--no-newline", "op://Wolfe.Lab/item/field"]);
        command.OutputRedacted.ShouldBeTrue();
    }

    [Fact]
    public async Task Read_SaysWhyWhenOpRefuses()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult(1, "", "[ERROR] 2026/09/18 could not read secret: \"item\" isn't an item in the \"Wolfe.Lab\" vault."));

        var failure = await Should.ThrowAsync<CommandFailedException>(
            () => new OnePasswordSecrets(_commands).Read(Reference, TestContext.Current.CancellationToken));

        failure.Message.ShouldContain("op://Wolfe.Lab/item/field");
        failure.Message.ShouldContain("isn't an item");
    }
}
