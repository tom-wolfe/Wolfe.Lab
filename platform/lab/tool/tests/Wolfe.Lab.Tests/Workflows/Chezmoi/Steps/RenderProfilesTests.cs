using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Clients.Chezmoi;
using Wolfe.Lab.Workflows.Chezmoi.Models;
using Wolfe.Lab.Workflows.Chezmoi.Steps;

namespace Wolfe.Lab.Tests.Workflows.Chezmoi.Steps;

public class RenderProfilesTests : IDisposable
{
    private readonly IChezmoi _chezmoi = Substitute.For<IChezmoi>();
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-render-checkout-");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IGit _git = Substitute.For<IGit>();

    public RenderProfilesTests()
    {
        var component = _checkout.CreateSubdirectory("platform").CreateSubdirectory("chezmoi").CreateSubdirectory("profiles");
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(component.FullName));
    }

    public void Dispose() => _checkout.Delete(recursive: true);

    private RenderProfiles Step() => new(_chezmoi, new Profiles(["macbook", "pi-node"]), _fileSystem, _git, Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_RendersEveryProfileFromTheCheckoutIntoItsOwnDirectory()
    {
        // The checkout's root, however deep the component: its .chezmoiroot is what names the source tree.
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory(_checkout.FullName));
        _chezmoi.Render(Arg.Any<IDirectory>(), Arg.Any<string>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<string>>(call => [$".zshrc-{call.Arg<string>()}"]);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        var rendered = result.Value.ShouldNotBeNull().Profiles;
        rendered.Select(p => p.Profile).ShouldBe(["macbook", "pi-node"]);
        rendered[0].Files.ShouldBe([".zshrc-macbook"]);
        rendered.Select(p => p.Directory.AbsolutePath).Distinct().Count().ShouldBe(2);
        await _chezmoi.Received(2).Render(Arg.Is<IDirectory>(d => d.AbsolutePath == _checkout.FullName), Arg.Any<string>(), Arg.Any<IDirectory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_RefusesAComponentOutsideACheckout()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns((IDirectory?)null);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        await _chezmoi.DidNotReceiveWithAnyArgs().Render(default!, default!, default!, TestContext.Current.CancellationToken);
    }
}
