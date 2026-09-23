using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Chezmoi;
using Wolfe.Lab.Build.Workflows.Chezmoi.Models;
using Wolfe.Lab.Build.Workflows.Chezmoi.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Chezmoi.Steps;

public class RenderProfilesTests
{
    private readonly IChezmoi _chezmoi = Substitute.For<IChezmoi>();

    [Fact]
    public async Task Run_RendersEveryProfileFromTheCheckoutIntoItsOwnDirectory()
    {
        // The checkout, two levels above the component: its .chezmoiroot is what names chezmoi/home.
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.ProjectRoot.Returns(new PhysicalDirectory("/lab/chezmoi/profiles"));
        _chezmoi.Render(Arg.Any<IDirectory>(), Arg.Any<string>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<string>>(call => [$".zshrc-{call.Arg<string>()}"]);

        var result = await new RenderProfiles(_chezmoi, new Profiles(["macbook", "pi-node"]), fileSystem, Substitute.For<IWorkflowLog>())
            .Run(TestContext.Current.CancellationToken);

        var rendered = result.Value.ShouldNotBeNull().Profiles;
        rendered.Select(p => p.Profile).ShouldBe(["macbook", "pi-node"]);
        rendered[0].Files.ShouldBe([".zshrc-macbook"]);
        rendered.Select(p => p.Directory.AbsolutePath).Distinct().Count().ShouldBe(2);
        await _chezmoi.Received(2).Render(Arg.Is<IDirectory>(d => d.AbsolutePath == "/lab"), Arg.Any<string>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>());
    }
}
