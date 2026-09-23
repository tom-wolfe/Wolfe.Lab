using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Workflows.Docker.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Docker.Steps;

public class ResolveComposeSecretsTests : IDisposable
{
    private readonly DirectoryInfo _source = Directory.CreateTempSubdirectory("lab-component-");
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public ResolveComposeSecretsTests()
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(_source.FullName));
        _secrets.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<string>().StartsWith("op://", StringComparison.Ordinal) ? $"value-of-{call.Arg<string>()}" : call.Arg<string>());
    }

    public void Dispose() => _source.Delete(recursive: true);

    private ResolveComposeSecrets Step() => new(_secrets, _fileSystem, Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_IsEmptyForAComponentWithoutSecrets()
    {
        var result = await Step().Run(TestContext.Current.CancellationToken);

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

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Variables["DB_PASSWORD"].ShouldBe("value-of-op://Wolfe.Lab/immich-postgres/credential");
    }

    [Fact]
    public async Task Run_FailsOnAMalformedFileBeforeReadingAnything()
    {
        await File.WriteAllTextAsync(Path.Combine(_source.FullName, ResolveComposeSecrets.FileName), "DB_PASSWORD=plain\n", TestContext.Current.CancellationToken);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        await _secrets.DidNotReceiveWithAnyArgs().Resolve("", TestContext.Current.CancellationToken);
    }
}
