using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Tests.Infrastructure.Releases;

// Against the real rsync: what it reports on a real run, not only a rehearsal, and what it writes,
// is what the report lists and the restart is decided by — 0.59.0 shipped with neither working.
public class RsyncInstallerIntegrationTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-rsync-");
    private readonly RsyncInstaller _installer = new(new ProcessCommandRunner());

    public void Dispose() => _root.Delete(recursive: true);

    private string Source(string path) => Path.Combine(_root.FullName, "component", path);

    private string Output => Path.Combine(_root.FullName, "release");

    private void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Source(path))!);
        File.WriteAllText(Source(path), content);
    }

    private Task<IReadOnlyList<string>> Publish() =>
        _installer.Install(new PhysicalDirectory(Path.Combine(_root.FullName, "component")), new PhysicalDirectory(Output), TestContext.Current.CancellationToken);

    private DateTimeOffset? Stamp() =>
        PublishArtifacts.Stamp([new Artifact(new PhysicalDirectory(Path.Combine(_root.FullName, "component")), new PhysicalDirectory(Output))]);

    [Fact]
    public async Task Install_ReportsWhatARealRunChangesAndWritesOnlyThat()
    {
        Write("compose.yaml", "services: {}");
        Write("config/a.yaml", "a");
        Write("ritten.json", "{}");
        Write("artifacts/report.md", "the last run's report");
        Write("temp/scratch", "scratch");

        (await Publish()).ShouldBe(["+ compose.yaml", "+ config/a.yaml"], ignoreOrder: true);
        File.Exists(Path.Combine(Output, "ritten.json")).ShouldBeFalse();
        Directory.Exists(Path.Combine(Output, "artifacts")).ShouldBeFalse();
        Directory.Exists(Path.Combine(Output, "temp")).ShouldBeFalse();
        var first = Stamp();

        // A fresh checkout: every file newer, none of them different.
        await Task.Delay(50, TestContext.Current.CancellationToken);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(_root.FullName, "component"), "*", SearchOption.AllDirectories))
        {
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow);
        }

        (await Publish()).ShouldBeEmpty();
        Stamp().ShouldBe(first);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        Write("config/a.yaml", "changed");
        (await Publish()).ShouldBe(["~ config/a.yaml"]);
        var changed = Stamp();
        changed.ShouldNotBeNull().ShouldBeGreaterThan(first!.Value);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        File.Delete(Source("config/a.yaml"));
        (await Publish()).ShouldBe(["- config/a.yaml"]);
        Stamp().ShouldNotBeNull().ShouldBeGreaterThan(changed.Value);
    }
}
