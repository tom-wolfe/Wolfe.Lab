using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Workflows.CaddyRoutes.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.CaddyRoutes.Steps;

public class GatherRoutesTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-gather-");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public GatherRoutesTests()
    {
        // The routes component sits two levels below the checkout, like every component.
        var component = _checkout.CreateSubdirectory("caddy").CreateSubdirectory("routes");
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(component.FullName));
        _fileSystem.Temp.Returns(new PhysicalDirectory(Path.Combine(component.FullName, "temp")));
    }

    public void Dispose() => _checkout.Delete(recursive: true);

    private void Route(string slice, string component, string content = "@x host x\n")
    {
        var directory = Directory.CreateDirectory(Path.Combine(_checkout.FullName, slice, component));
        File.WriteAllText(Path.Combine(directory.FullName, GatherRoutes.Snippet), content);
    }

    private GatherRoutes Step() => new(_fileSystem, Substitute.For<IWorkflowLog>());

    private static string Staged(IDirectory staging, string name) => Path.Combine(staging.AbsolutePath, name + GatherRoutes.Extension);

    [Fact]
    public void Run_StagesEveryComponentsRouteUnderItsSliceAndComponentName()
    {
        Route("jellyfin", "compose");
        Route("mail", "watcher");

        var staged = Step().Run().Value.ShouldNotBeNull();

        staged.Names.ShouldBe(["jellyfin-compose", "mail-watcher"]);
        File.Exists(Staged(staged.Directory, "jellyfin-compose")).ShouldBeTrue();
        File.Exists(Staged(staged.Directory, "mail-watcher")).ShouldBeTrue();
    }

    [Fact]
    public void Run_GathersFromTheCheckoutSoOneComponentDoesNotWaitOnAnother()
    {
        Route("ollama", "server", "@ai host ai.twolfe.dev\n");

        var staged = Step().Run().Value.ShouldNotBeNull();

        // ollama installs no stack of its own — its route arrives only because this step
        // reads the checkout rather than the install root.
        File.ReadAllText(Staged(staged.Directory, "ollama-server")).ShouldContain("ai.twolfe.dev");
    }

    [Fact]
    public void Run_StartsFromAnEmptyStagingDirectoryEachTime()
    {
        Route("jellyfin", "compose");
        var first = Step().Run().Value.ShouldNotBeNull();
        File.Delete(Path.Combine(_checkout.FullName, "jellyfin", "compose", GatherRoutes.Snippet));

        var second = Step().Run().Value.ShouldNotBeNull();

        second.Names.ShouldBeEmpty();
        File.Exists(Staged(first.Directory, "jellyfin-compose")).ShouldBeFalse();
    }

    [Fact]
    public void Run_IgnoresAComponentThatPublishesNoRoute()
    {
        Directory.CreateDirectory(Path.Combine(_checkout.FullName, "restic", "repositories"));
        Route("jellyfin", "compose");

        Step().Run().Value.ShouldNotBeNull().Names.ShouldBe(["jellyfin-compose"]);
    }
}
