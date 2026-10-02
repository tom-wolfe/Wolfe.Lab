using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Tests.Application.Releases;

public class PublishArtifactsTests : IDisposable
{
    private readonly DirectoryInfo _node = Directory.CreateTempSubdirectory("lab-published-");

    public void Dispose() => _node.Delete(recursive: true);

    private static readonly DateTime Old = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private Artifact Output(string name)
    {
        var output = _node.CreateSubdirectory(name);
        Directory.SetLastWriteTimeUtc(output.FullName, Old);
        return new Artifact(new PhysicalDirectory(_node.FullName), new PhysicalDirectory(output.FullName));
    }

    [Fact]
    public void Stamp_IsTheNewestWriteUnderAnyOutput()
    {
        var alloy = Output("alloy");
        var nested = Directory.CreateDirectory(Path.Combine(alloy.Output.AbsolutePath, "modules")).FullName;
        File.WriteAllText(Path.Combine(nested, "a.alloy"), "");
        var newest = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(nested, "a.alloy"), newest);
        Directory.SetLastWriteTimeUtc(nested, Old);
        Directory.SetLastWriteTimeUtc(alloy.Output.AbsolutePath, Old);

        PublishArtifacts.Stamp([alloy, Output("other")]).ShouldBe(new DateTimeOffset(newest));
    }

    [Fact]
    public void Stamp_CountsADirectoryWhoseEntriesChanged()
    {
        // A deleted file leaves nothing behind but its directory's time.
        var alloy = Output("alloy");
        var removed = new DateTime(2026, 9, 29, 13, 0, 0, DateTimeKind.Utc);
        Directory.SetLastWriteTimeUtc(alloy.Output.AbsolutePath, removed);

        PublishArtifacts.Stamp([alloy]).ShouldBe(new DateTimeOffset(removed));
    }

    [Fact]
    public void Stamp_IsNullWithNothingPublished() =>
        PublishArtifacts.Stamp([]).ShouldBeNull();
}
