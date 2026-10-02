using Ritten.Engine.FileSystem;
using Wolfe.Lab.Clients.Restic.Steps;
using Wolfe.Lab.Values;

namespace Wolfe.Lab.Tests.Clients.Restic.Steps;

public class ResolveRepositoryTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-checkout-");
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly string _envFile;

    public ResolveRepositoryTests()
    {
        // A backup component two levels below the checkout, as components sat before areas.
        _checkout.CreateSubdirectory(".git");
        var component = _checkout.CreateSubdirectory("files").CreateSubdirectory("backup");
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(component.FullName));
        _envFile = Path.Combine(_checkout.CreateSubdirectory(Service.Restic.Name).FullName, ResolveRepository.FileName);
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
    public async Task Run_FailsWhenTheCheckoutHasNoResticService()
    {
        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_FindsTheServiceInsideAnArea()
    {
        // The layout areas give it: the service under platform/, the component under another area.
        File.Delete(_envFile);
        var component = _checkout.CreateSubdirectory("media").CreateSubdirectory("jellyfin").CreateSubdirectory("backup");
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(component.FullName));
        var service = _checkout.CreateSubdirectory("platform").CreateSubdirectory(Service.Restic.Name);
        await File.WriteAllTextAsync(Path.Combine(service.FullName, ResolveRepository.FileName),
            "RESTIC_REPOSITORY=sftp:macmini:/Volumes/Data2/restic\nRESTIC_PASSWORD=\"op://Wolfe.Lab/restic-repo/password\"\n",
            TestContext.Current.CancellationToken);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Location.ShouldBe("sftp:macmini:/Volumes/Data2/restic");
    }

    [Fact]
    public async Task Run_LooksNoFurtherThanTheCheckout()
    {
        // A service beside the checkout, not in it: finding it would mean the walk had gone on
        // through whatever holds the checkout — on a node, its home directory.
        var outside = Directory.CreateTempSubdirectory("lab-outside-");
        try
        {
            var checkout = outside.CreateSubdirectory("checkout");
            checkout.CreateSubdirectory(".git");
            var component = checkout.CreateSubdirectory("files").CreateSubdirectory("backup");
            _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(component.FullName));
            var stray = outside.CreateSubdirectory("elsewhere").CreateSubdirectory(Service.Restic.Name);
            await File.WriteAllTextAsync(Path.Combine(stray.FullName, ResolveRepository.FileName),
                "RESTIC_REPOSITORY=sftp:macmini:/Volumes/Data2/restic\n", TestContext.Current.CancellationToken);

            var result = await Step().Run(TestContext.Current.CancellationToken);

            result.Outcome.IsFailure.ShouldBeTrue();
        }
        finally
        {
            outside.Delete(recursive: true);
        }
    }
}
