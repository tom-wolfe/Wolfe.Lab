using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Caddy.Steps;
using Wolfe.Lab.Build.Deploy.Models;

namespace Wolfe.Lab.Build.Tests.Caddy.Steps;

public class GatherRoutesTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-gather-");

    public void Dispose() => _root.Delete(recursive: true);

    private string Checkout => Path.Combine(_root.FullName, "repo");
    private string Installs => Path.Combine(_root.FullName, "installs");

    private void Route(string slice, string content = "@x host x\n")
    {
        Directory.CreateDirectory(Path.Combine(Checkout, slice));
        File.WriteAllText(Path.Combine(Checkout, slice, "caddy.caddyfile"), content);
    }

    private void SliceWithoutARoute(string slice) => Directory.CreateDirectory(Path.Combine(Checkout, slice));

    private StepResult Run(bool dryRun = false)
    {
        Directory.CreateDirectory(Path.Combine(Checkout, "caddy"));
        var slice = new Slice(
            "caddy",
            new PhysicalDirectory(Path.Combine(Checkout, "caddy")),
            new PhysicalDirectory(Path.Combine(Installs, "caddy")));

        return new GatherRoutes(new WorkflowJob("caddy", "deploy", dryRun), Substitute.For<IWorkflowLog>()).Run(slice);
    }

    private string Gathered(string slice) => Path.Combine(Installs, slice, "caddy.caddyfile");

    [Fact]
    public void Run_PutsEverySlicesRouteWhereTheFrontDoorReadsThem()
    {
        Route("jellyfin");
        Route("forgejo");

        Run().IsFailure.ShouldBeFalse();

        File.Exists(Gathered("jellyfin")).ShouldBeTrue();
        File.Exists(Gathered("forgejo")).ShouldBeTrue();
    }

    [Fact]
    public void Run_GathersFromTheCheckoutSoOneSliceDoesNotWaitOnAnother()
    {
        Route("ollama", "@ai host ai.twolfe.dev\n");

        Run();

        // ollama installs nothing of its own — its route arrives only because this step
        // reads the checkout rather than the install root.
        File.ReadAllText(Gathered("ollama")).ShouldContain("ai.twolfe.dev");
    }

    [Fact]
    public void Run_OverwritesARouteThatChanged()
    {
        Route("jellyfin", "old\n");
        Run();
        Route("jellyfin", "new\n");

        Run();

        File.ReadAllText(Gathered("jellyfin")).ShouldBe("new\n");
    }

    [Fact]
    public void Run_IgnoresASliceThatPublishesNoRoute()
    {
        SliceWithoutARoute("restic");
        Route("jellyfin");

        Run();

        Directory.Exists(Path.Combine(Installs, "restic")).ShouldBeFalse();
    }

    [Fact]
    public void Run_WritesNothingOnARehearsal()
    {
        Route("jellyfin");

        Run(dryRun: true).IsFailure.ShouldBeFalse();

        File.Exists(Gathered("jellyfin")).ShouldBeFalse();
    }
}
