using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Tests.Secrets;

public class EnvFileTests
{
    [Fact]
    public void Parse_KeepsLiteralsAndRecognisesReferences()
    {
        var result = EnvFile.Parse(
            "# the local repository\nRESTIC_REPOSITORY=/Volumes/Data2/restic\nRESTIC_PASSWORD=\"op://Wolfe.Lab/restic-repo/password\"\n",
            "restic.env");

        var entries = result.Value.ShouldNotBeNull();
        entries["RESTIC_REPOSITORY"].ShouldBe(new EnvValue("/Volumes/Data2/restic", null));
        entries["RESTIC_PASSWORD"].ShouldBe(new EnvValue(null, SecretReference.From("op://Wolfe.Lab/restic-repo/password")));
    }

    [Fact]
    public void Parse_NamesTheLineThatIsNotAnAssignment()
    {
        var result = EnvFile.Parse("RESTIC_REPOSITORY=/r\nnot an assignment\n", "restic.env");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldStartWith("restic.env:2:");
    }

    [Fact]
    public async Task Resolve_ReadsReferencesThroughTheVaultAndPassesLiteralsThrough()
    {
        var secrets = Substitute.For<ISecrets>();
        secrets.Read(SecretReference.From("op://any/item/field"), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(call => $"value-of-{call.Arg<SecretReference>().Value}");
        var entries = EnvFile.Parse("A=literal\nB=\"op://Wolfe.Lab/item/field\"\n", "x.env").Value!;

        var variables = await entries.Resolve(secrets, TestContext.Current.CancellationToken);

        variables["A"].ShouldBe("literal");
        variables["B"].ShouldBe("value-of-op://Wolfe.Lab/item/field");
    }
}
