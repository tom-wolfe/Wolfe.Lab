using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Application.Workflows.CaddyRoutes.Steps;

namespace Wolfe.Lab.Tests.Application.Workflows.CaddyRoutes.Steps;

public class GatherRoutesTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-gather-");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IGit _git = Substitute.For<IGit>();

    public GatherRoutesTests()
    {
        // The routes component sits three levels below the checkout — area, service, component.
        var component = _checkout.CreateSubdirectory("network").CreateSubdirectory("caddy").CreateSubdirectory("routes");
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(component.FullName));
        _fileSystem.Temp.Returns(new PhysicalDirectory(Path.Combine(component.FullName, "temp")));
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory(_checkout.FullName));
    }

    public void Dispose() => _checkout.Delete(recursive: true);

    private void Route(string component, string content = "@x host x\n")
    {
        var directory = Directory.CreateDirectory(Path.Combine(_checkout.FullName, component));
        File.WriteAllText(Path.Combine(directory.FullName, GatherRoutes.Snippet), content);
    }

    private GatherRoutes Step() => new(_fileSystem, _git, Substitute.For<IWorkflowLog>());

    private static string Staged(IDirectory staging, string name) => Path.Combine(staging.AbsolutePath, name + GatherRoutes.Extension);

    [Fact]
    public async Task Run_StagesEveryComponentsRouteUnderItsPathFromTheCheckout()
    {
        Route("media/jellyfin/compose");
        Route("personal/mail/watcher");

        var staged = (await Step().Run(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        staged.Names.ShouldBe(["media-jellyfin-compose", "personal-mail-watcher"]);
        File.Exists(Staged(staged.Directory, "media-jellyfin-compose")).ShouldBeTrue();
        File.Exists(Staged(staged.Directory, "personal-mail-watcher")).ShouldBeTrue();
    }

    [Fact]
    public async Task Run_GathersAtAnyDepthSoServicesCanMoveOneAreaAtATime()
    {
        Route("jellyfin/compose");
        Route("monitoring/gatus/compose");

        (await Step().Run(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().Names.ShouldBe(["jellyfin-compose", "monitoring-gatus-compose"]);
    }

    [Fact]
    public async Task Run_IgnoresHiddenDirectories()
    {
        Route(".git/stray");
        Route("media/jellyfin/compose");

        (await Step().Run(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().Names.ShouldBe(["media-jellyfin-compose"]);
    }

    [Fact]
    public async Task Run_RefusesAComponentOutsideACheckout()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns((IDirectory?)null);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("not in a git checkout");
    }

    [Fact]
    public async Task Run_GathersFromTheCheckoutSoOneComponentDoesNotWaitOnAnother()
    {
        Route("ai/ollama/mini", "@ai host ai.twolfe.dev\n");

        var staged = (await Step().Run(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        // ollama installs no stack of its own — its route arrives only because this step
        // reads the checkout rather than the install root.
        File.ReadAllText(Staged(staged.Directory, "ai-ollama-mini")).ShouldContain("ai.twolfe.dev");
    }

    [Fact]
    public async Task Run_StartsFromAnEmptyStagingDirectoryEachTime()
    {
        Route("media/jellyfin/compose");
        var first = (await Step().Run(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();
        File.Delete(Path.Combine(_checkout.FullName, "media", "jellyfin", "compose", GatherRoutes.Snippet));

        var second = (await Step().Run(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        second.Names.ShouldBeEmpty();
        File.Exists(Staged(first.Directory, "media-jellyfin-compose")).ShouldBeFalse();
    }

    [Fact]
    public async Task Run_IgnoresAComponentThatPublishesNoRoute()
    {
        Directory.CreateDirectory(Path.Combine(_checkout.FullName, "platform", "restic", "repositories"));
        Route("media/jellyfin/compose");

        (await Step().Run(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().Names.ShouldBe(["media-jellyfin-compose"]);
    }
}
