using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Slices;
using Wolfe.Lab.Build.Slices.Steps;

namespace Wolfe.Lab.Build.Tests.Slices.Steps;

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

    private static CheckVolumes Step(params DirectoryInfo[] volumes) =>
        new(new RequiredVolumes([.. volumes.Select(v => new PhysicalDirectory(v.FullName))]), Substitute.For<IWorkflowLog>());

    [Fact]
    public void Run_PassesWhenEverySentinelIsPresent()
    {
        Step(_mounted).Run().IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void Run_PassesWithNothingToCheck()
    {
        Step().Run().IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void Run_NamesEachVolumeWithoutItsSentinel()
    {
        var result = Step(_mounted, _shadow).Run();

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain(_shadow.FullName);
    }
}
