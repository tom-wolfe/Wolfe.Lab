using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Tests.Deploy.Steps;

public class ResolveComposeSecretsTests : IDisposable
{
    private readonly DirectoryInfo _source = Directory.CreateTempSubdirectory("lab-slice-");
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();
    private readonly Slice _slice;

    public ResolveComposeSecretsTests()
    {
        _slice = new Slice("immich", new PhysicalDirectory(_source.FullName), new PhysicalDirectory(Path.Combine(_source.FullName, "release")));
        _secrets.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<string>().StartsWith("op://", StringComparison.Ordinal) ? $"value-of-{call.Arg<string>()}" : call.Arg<string>());
    }

    public void Dispose() => _source.Delete(recursive: true);

    private ResolveComposeSecrets Step() => new(_secrets, Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_IsEmptyForASliceWithoutSecrets()
    {
        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Variables.ShouldBeEmpty();
        await _secrets.DidNotReceiveWithAnyArgs().Resolve("", TestContext.Current.CancellationToken);
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
        await _secrets.DidNotReceiveWithAnyArgs().Resolve("", TestContext.Current.CancellationToken);
    }
}
