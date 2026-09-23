using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Clients.Restic.Steps;

namespace Wolfe.Lab.Build.Tests.Clients.Restic.Steps;

public class ResolveRepositoryTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-checkout-");
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly string _envFile;

    public ResolveRepositoryTests()
    {
        // A backup component two levels below the checkout, where every component sits.
        var component = _checkout.CreateSubdirectory("files").CreateSubdirectory("backup");
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(component.FullName));
        _envFile = Path.Combine(_checkout.CreateSubdirectory(ResticEnvironment.SliceName).FullName, ResolveRepository.FileName);
        _secrets.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<string>().StartsWith("op://", StringComparison.Ordinal) ? $"value-of-{call.Arg<string>()}" : call.Arg<string>());
    }

    public void Dispose() => _checkout.Delete(recursive: true);

    private ResolveRepository Step() => new(_secrets, _fileSystem, Substitute.For<IWorkflowLog>());

    private string LocalRepository(bool initialised)
    {
        var repository = _checkout.CreateSubdirectory("restic-repo");
        if (initialised)
        {
            File.WriteAllText(Path.Combine(repository.FullName, ResolveRepository.ConfigFile), "{}");
        }

        return repository.FullName;
    }

    [Fact]
    public async Task Run_ResolvesTheEnvFileIntoResticsEnvironment()
    {
        var repository = LocalRepository(initialised: true);
        await File.WriteAllTextAsync(_envFile, $"RESTIC_REPOSITORY={repository}\nRESTIC_PASSWORD=\"op://Wolfe.Lab/restic-repo/password\"\n", TestContext.Current.CancellationToken);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        var resolved = result.Value.ShouldNotBeNull();
        resolved.Location.ShouldBe(repository);
        resolved.IsLocal.ShouldBeTrue();
        resolved.Environment["RESTIC_PASSWORD"].ShouldBe("value-of-op://Wolfe.Lab/restic-repo/password");
    }

    [Fact]
    public async Task Run_RefusesALocalPathThatHoldsNoRepository()
    {
        var repository = LocalRepository(initialised: false);
        await File.WriteAllTextAsync(_envFile, $"RESTIC_REPOSITORY={repository}\nRESTIC_PASSWORD=\"op://Wolfe.Lab/restic-repo/password\"\n", TestContext.Current.CancellationToken);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("No restic repository");
    }

    [Fact]
    public async Task Run_TakesARemoteRepositoryOnTrust()
    {
        await File.WriteAllTextAsync(_envFile, "RESTIC_REPOSITORY=sftp:macmini:/Volumes/Data2/restic\nRESTIC_PASSWORD=\"op://Wolfe.Lab/restic-repo/password\"\n", TestContext.Current.CancellationToken);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().IsLocal.ShouldBeFalse();
    }

    [Fact]
    public async Task Run_FailsWhenTheFileNamesNoRepository()
    {
        await File.WriteAllTextAsync(_envFile, "RESTIC_PASSWORD=\"op://Wolfe.Lab/restic-repo/password\"\n", TestContext.Current.CancellationToken);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        await _secrets.DidNotReceiveWithAnyArgs().Resolve("", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_FailsWhenTheCheckoutHasNoResticSlice()
    {
        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
    }
}
