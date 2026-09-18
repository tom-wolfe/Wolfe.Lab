using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Immich.Models;
using Wolfe.Lab.Build.Immich.Steps;

namespace Wolfe.Lab.Build.Tests.Immich.Steps;

public class ResolveTakeoutTests : IDisposable
{
    private readonly DirectoryInfo _takeout = Directory.CreateTempSubdirectory("lab-takeout-");

    public void Dispose() => _takeout.Delete(recursive: true);

    private ResolveTakeout Step(string path) => new(new TakeoutLocation(new PhysicalDirectory(path)), Substitute.For<IWorkflowLog>());

    [Fact]
    public void Run_CountsTheParts()
    {
        File.WriteAllText(Path.Combine(_takeout.FullName, "takeout-002.zip"), "");
        File.WriteAllText(Path.Combine(_takeout.FullName, "takeout-001.zip"), "");
        File.WriteAllText(Path.Combine(_takeout.FullName, "notes.txt"), "");

        var result = Step(_takeout.FullName).Run();

        result.Value.ShouldNotBeNull().Parts.ShouldBe(["takeout-001.zip", "takeout-002.zip"]);
    }

    [Fact]
    public void Run_FailsWithNoParts()
    {
        Step(_takeout.FullName).Run().Outcome.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Run_FailsWhenTheDirectoryIsMissing()
    {
        Step(Path.Combine(_takeout.FullName, "missing")).Run().Outcome.IsFailure.ShouldBeTrue();
    }
}
