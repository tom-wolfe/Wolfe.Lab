using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Workflows.Tofu.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Tofu.Steps;

public class ResolveEnvironmentTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-checkout-");
    private readonly DirectoryInfo _root;

    public ResolveEnvironmentTests()
    {
        _checkout.CreateSubdirectory(".git");
        _root = _checkout.CreateSubdirectory("network").CreateSubdirectory("caddy").CreateSubdirectory("tofu");
    }

    public void Dispose() => _checkout.Delete(recursive: true);

    private ResolveEnvironment Step()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.ProjectRoot.Returns(new PhysicalDirectory(_root.FullName));
        return new ResolveEnvironment(fileSystem, Substitute.For<IWorkflowLog>());
    }

    // Garage's, in its area, where it lives from now on.
    private string WriteState(params string[] directory)
    {
        var path = Path.Combine(Directory.CreateDirectory(Path.Combine([_checkout.FullName, .. directory.Length > 0 ? directory : ["platform", "garage"]])).FullName, ResolveEnvironment.StateFile);
        File.WriteAllText(path, "AWS_ACCESS_KEY_ID=\"op://Wolfe.Lab/garage-tofu-state-key/username\"\n");
        return path;
    }

    [Fact]
    public void Run_FindsTheSharedStateFileAboveTheRoot()
    {
        var state = WriteState();

        var environment = Step().Run().Value.ShouldNotBeNull();

        environment.EnvFiles.ShouldHaveSingleItem().AbsolutePath.ShouldBe(state);
        environment.VarFile.ShouldBeNull();
    }

    [Fact]
    public void Run_AddsTheRootsOwnSecretsAfterTheSharedFile()
    {
        // Second, so a root can override a shared entry; and only when it exists, because a
        // root with no provider credential declares none.
        var state = WriteState();
        var secrets = Path.Combine(_root.FullName, ResolveEnvironment.SecretsFile);
        File.WriteAllText(secrets, "TF_VAR_netlify_token=\"op://Wolfe.Lab/netlify-pat/credential\"\n");

        var environment = Step().Run().Value.ShouldNotBeNull();

        environment.EnvFiles.Select(f => f.AbsolutePath).ShouldBe([state, secrets]);
    }

    [Fact]
    public void Run_RefusesARootWithNoStateFileAboveIt()
    {
        var result = Step().Run();

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain(ResolveEnvironment.StateFile);
    }

    [Fact]
    public void Run_StillFindsTheStateFileWhereItWasUntilItMoves()
    {
        var state = WriteState("build");

        Step().Run().Value.ShouldNotBeNull().EnvFiles.ShouldHaveSingleItem().AbsolutePath.ShouldBe(state);
    }

    [Fact]
    public void Run_PrefersGaragesCopyOverTheOneItReplaces()
    {
        WriteState("build");
        var garage = WriteState();

        Step().Run().Value.ShouldNotBeNull().EnvFiles.ShouldHaveSingleItem().AbsolutePath.ShouldBe(garage);
    }
}
