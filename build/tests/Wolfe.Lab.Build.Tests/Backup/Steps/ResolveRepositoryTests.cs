using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Backup.Steps;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Restic;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Tests.Backup.Steps;

public class ResolveRepositoryTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-checkout-");
    private readonly ISecrets _secrets = Substitute.For<ISecrets>();
    private readonly Slice _slice;
    private readonly string _envFile;

    public ResolveRepositoryTests()
    {
        var source = _checkout.CreateSubdirectory("files");
        _slice = new Slice("files", new PhysicalDirectory(source.FullName), new PhysicalDirectory(Path.Combine(_checkout.FullName, "release", "files")));
        _envFile = Path.Combine(_checkout.CreateSubdirectory(ResticEnvironment.SliceName).FullName, ResolveRepository.FileName);
        _secrets.Read(SecretReference.From("op://any/item/field"), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(call => $"value-of-{call.Arg<SecretReference>().Value}");
    }

    public void Dispose() => _checkout.Delete(recursive: true);

    private ResolveRepository Step() => new(_secrets, Substitute.For<IWorkflowLog>());

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

        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

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

        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("No restic repository");
    }

    [Fact]
    public async Task Run_TakesARemoteRepositoryOnTrust()
    {
        await File.WriteAllTextAsync(_envFile, "RESTIC_REPOSITORY=sftp:macmini:/Volumes/Data2/restic\nRESTIC_PASSWORD=\"op://Wolfe.Lab/restic-repo/password\"\n", TestContext.Current.CancellationToken);

        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().IsLocal.ShouldBeFalse();
    }

    [Fact]
    public async Task Run_FailsWhenTheFileNamesNoRepository()
    {
        await File.WriteAllTextAsync(_envFile, "RESTIC_PASSWORD=\"op://Wolfe.Lab/restic-repo/password\"\n", TestContext.Current.CancellationToken);

        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        await _secrets.DidNotReceiveWithAnyArgs().Read(SecretReference.From("op://a/b/c"), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_FailsWhenTheCheckoutHasNoResticSlice()
    {
        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
    }
}
