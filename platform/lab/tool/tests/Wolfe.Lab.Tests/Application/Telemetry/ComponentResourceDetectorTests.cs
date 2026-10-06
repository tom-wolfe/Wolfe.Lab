using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Telemetry;

namespace Wolfe.Lab.Tests.Application.Telemetry;

// Against this repository's own declarations, as a run in one of its directories finds them.
public class ComponentResourceDetectorTests
{
    [Fact]
    public void Detect_NamesTheComponentOfADirectoryThatDeclaresOne() =>
        Attributes("monitoring/alloy/forwarder").ShouldBe(new Dictionary<string, object>
        {
            ["lab.area"] = "monitoring",
            ["lab.service"] = "alloy",
            ["lab.component"] = "forwarder"
        });

    [Fact]
    public void Detect_NamesOnlyWhatAStacksComponentsShare() =>
        Attributes("monitoring/grafana/compose").ShouldBe(new Dictionary<string, object>
        {
            ["lab.area"] = "monitoring",
            ["lab.service"] = "grafana"
        });

    [Fact]
    public void Detect_NamesNothingWhereNothingIsDeclared() =>
        Attributes("platform/lab/tool").ShouldBeEmpty();

    private static Dictionary<string, object> Attributes(string directory)
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.ProjectRoot.Returns(new PhysicalDirectory(Path.Combine(Checkout(), directory)));
        return new ComponentResourceDetector(RealClients.Git.InRepository(fileSystem.ProjectRoot), fileSystem).Detect().Attributes
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
    }

    private static string Checkout()
    {
        for (var directory = AppContext.BaseDirectory; directory is not null; directory = Path.GetDirectoryName(directory))
        {
            if (Directory.Exists(Path.Combine(directory, ".git")) || File.Exists(Path.Combine(directory, ".git")))
            {
                return directory;
            }
        }

        throw new InvalidOperationException($"{AppContext.BaseDirectory} is not in a checkout.");
    }
}
