using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Tests.Deploy.Steps;

public class ResolveComposeSecretsTests : IDisposable
{
    private readonly DirectoryInfo _source = Directory.CreateTempSubdirectory("lab-slice-");
    private readonly ISecrets _secrets = Substitute.For<ISecrets>();
    private readonly Slice _slice;

    public ResolveComposeSecretsTests()
    {
        _slice = new Slice("immich", new PhysicalDirectory(_source.FullName), new PhysicalDirectory(Path.Combine(_source.FullName, "release")));
        // A matcher would hand the substitute a default value object, which Vogen refuses to
        // read, so the stub is configured with a real reference and answers for any.
        _secrets.Read(SecretReference.From("op://any/item/field"), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(call => $"value-of-{call.Arg<SecretReference>().Value}");
    }

    public void Dispose() => _source.Delete(recursive: true);

    private ResolveComposeSecrets Step() => new(_secrets, Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_IsEmptyForASliceWithoutSecrets()
    {
        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Variables.ShouldBeEmpty();
        await _secrets.DidNotReceiveWithAnyArgs().Read(SecretReference.From("op://a/b/c"), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_ResolvesEveryReferenceTheFileNames()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_source.FullName, ResolveComposeSecrets.FileName),
            "DB_PASSWORD=\"op://Wolfe.Lab/immich-postgres/credential\"\n",
            TestContext.Current.CancellationToken);

        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Variables["DB_PASSWORD"].ShouldBe("value-of-op://Wolfe.Lab/immich-postgres/credential");
    }

    [Fact]
    public async Task Run_FailsOnAMalformedFileBeforeReadingAnything()
    {
        await File.WriteAllTextAsync(Path.Combine(_source.FullName, ResolveComposeSecrets.FileName), "DB_PASSWORD=plain\n", TestContext.Current.CancellationToken);

        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        await _secrets.DidNotReceiveWithAnyArgs().Read(SecretReference.From("op://a/b/c"), TestContext.Current.CancellationToken);
    }
}
