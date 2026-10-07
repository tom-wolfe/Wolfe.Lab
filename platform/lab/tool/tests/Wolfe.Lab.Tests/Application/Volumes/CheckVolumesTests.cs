using Wolfe.Lab.Application.Volumes;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Volumes;

public class CheckVolumesTests : IDisposable
{
    private readonly DirectoryInfo _mounted = Directory.CreateTempSubdirectory("lab-volume-");
    private readonly DirectoryInfo _shadow = Directory.CreateTempSubdirectory("lab-shadow-");

    public CheckVolumesTests() => File.WriteAllText(Path.Combine(_mounted.FullName, CheckVolumes.Sentinel), "");

    public void Dispose()
    {
        _mounted.Delete(recursive: true);
        _shadow.Delete(recursive: true);
    }

    // A stack whose server requires the volumes.
    private static DeploymentUnit Unit(params DirectoryInfo[] volumes)
    {
        var catalog = Catalogs.Of("personal/immich/compose", Catalogs.Docker("server", "immich-server"));
        catalog.Services.Single().Components.Single().RequiresVolumes = [.. volumes.Select(volume => HostPath.From(volume.FullName))];
        return catalog.DeploymentUnitAt(RepositoryPath.From("personal/immich/compose")).ShouldNotBeNull().Value.ShouldNotBeNull();
    }

    private static StepResult Check(DeploymentUnit unit) => new CheckVolumes(Substitute.For<IWorkflowLog>()).Run(unit);

    [Fact]
    public void Run_PassesWhenEverySentinelIsPresent() =>
        Check(Unit(_mounted)).IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_PassesWithNothingToCheck() =>
        Check(Unit()).IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_NamesEachVolumeWithoutItsSentinel()
    {
        var result = Check(Unit(_mounted, _shadow));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain(_shadow.FullName);
    }
}
