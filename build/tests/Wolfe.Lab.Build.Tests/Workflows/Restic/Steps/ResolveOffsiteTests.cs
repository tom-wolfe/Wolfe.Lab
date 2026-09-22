using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Slices;
using Wolfe.Lab.Build.Workflows.Restic.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Restic.Steps;

public class ResolveOffsiteTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-checkout-");
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();
    private readonly Slice _slice;
    private readonly string _envFile;

    public ResolveOffsiteTests()
    {
        var source = _checkout.CreateSubdirectory(ResticEnvironment.SliceName);
        _slice = new Slice("restic", new PhysicalDirectory(source.FullName), new PhysicalDirectory(Path.Combine(_checkout.FullName, "release", "restic")));
        _envFile = Path.Combine(source.FullName, ResolveOffsite.FileName);
        _secrets.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<string>().StartsWith("op://", StringComparison.Ordinal) ? $"value-of-{call.Arg<string>()}" : call.Arg<string>());
    }

    public void Dispose() => _checkout.Delete(recursive: true);

    private ResolveOffsite Step() => new(_secrets, Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_ResolvesTheDestinationAndItsSource()
    {
        await File.WriteAllTextAsync(
            _envFile,
            "RESTIC_REPOSITORY=\"op://Wolfe.Lab/restic-b2/repository\"\nRESTIC_PASSWORD=\"op://Wolfe.Lab/restic-repo/password\"\nRESTIC_FROM_REPOSITORY=/Volumes/Data2/restic\n",
            TestContext.Current.CancellationToken);

        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

        var offsite = result.Value.ShouldNotBeNull();
        offsite.Repository.Location.ShouldBe("value-of-op://Wolfe.Lab/restic-b2/repository");
        offsite.Repository.Environment["RESTIC_FROM_REPOSITORY"].ShouldBe("/Volumes/Data2/restic");
    }

    [Fact]
    public async Task Run_RefusesADestinationWithoutASource()
    {
        await File.WriteAllTextAsync(_envFile, "RESTIC_REPOSITORY=s3:https://b2/bucket\nRESTIC_PASSWORD=\"op://Wolfe.Lab/restic-repo/password\"\n", TestContext.Current.CancellationToken);

        var result = await Step().Run(_slice, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
    }
}
