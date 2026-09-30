using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Clients.Packages;
using Wolfe.Lab.Clients.Packages.Steps;

namespace Wolfe.Lab.Tests.Clients.Packages.Steps;

// PATH is the process's, so these must not run beside anything else that reads it.
[Collection(nameof(ProcessPath))]
[System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
public class EnsureToolsTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-tools-checkout-");
    private readonly DirectoryInfo _package = Directory.CreateTempSubdirectory("lab-tofu-");
    private readonly IPackageInstaller _installer = Substitute.For<IPackageInstaller>();
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly string? _path = Environment.GetEnvironmentVariable("PATH");

    public EnsureToolsTests()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory(_checkout.FullName));
        Directory.CreateDirectory(Path.Combine(_checkout.FullName, ".config"));
        File.WriteAllText(Path.Combine(_checkout.FullName, ".config", "lab-tools.json"), """
            {
              "tools": {
                "tofu": {
                  "github": "opentofu/opentofu",
                  "version": "1.12.6",
                  "checksums": "tofu_{version}_SHA256SUMS",
                  "assets": { "darwin-arm64": "tofu_{version}_darwin_arm64.tar.gz", "linux-arm64": "tofu_{version}_linux_arm64.tar.gz" }
                }
              }
            }
            """);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("PATH", _path);
        _checkout.Delete(recursive: true);
        _package.Delete(recursive: true);
    }

    private Task<StepResult> Ensure(PackageOutcome outcome, bool dryRun = false, params string[] tools)
    {
        _installer.Install(Arg.Any<Package>(), Arg.Any<CancellationToken>())
            .Returns(call => new InstalledPackage(call.Arg<Package>(), new PhysicalDirectory(_package.FullName), outcome));
        return new EnsureTools(new RequiredTools(tools), _git, _installer, new WorkflowJob("tofu", "deploy", dryRun, AutoApprove: true),
                Report(), Substitute.For<IWorkflowLog>())
            .Run(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_PutsThePinnedToolFirstOnThePath()
    {
        File.WriteAllText(Path.Combine(_package.FullName, "tofu"), "");

        (await Ensure(PackageOutcome.Installed, tools: "tofu")).IsFailure.ShouldBeFalse();

        Environment.GetEnvironmentVariable("PATH").ShouldBe($"{_package.FullName}{Path.PathSeparator}{_path}");
        (File.GetUnixFileMode(Path.Combine(_package.FullName, "tofu")) & UnixFileMode.UserExecute).ShouldBe(UnixFileMode.UserExecute);
    }

    [Fact]
    public async Task Run_PutsANestedCommandsDirectoryOnThePath()
    {
        File.WriteAllText(Path.Combine(_checkout.FullName, ".config", "lab-tools.json"), """
            { "tools": { "shellcheck": { "github": "koalaman/shellcheck", "version": "0.11.0", "bin": "shellcheck-v{version}",
              "assets": { "darwin-arm64": "a.tar.gz", "linux-arm64": "a.tar.gz" } } } }
            """);
        var bin = Directory.CreateDirectory(Path.Combine(_package.FullName, "shellcheck-v0.11.0")).FullName;
        File.WriteAllText(Path.Combine(bin, "shellcheck"), "");

        (await Ensure(PackageOutcome.Installed, tools: "shellcheck")).IsFailure.ShouldBeFalse();

        Environment.GetEnvironmentVariable("PATH").ShouldBe($"{bin}{Path.PathSeparator}{_path}");
    }

    [Fact]
    public async Task Run_LeavesAToolTheManifestDoesNotPinToTheNode()
    {
        (await Ensure(PackageOutcome.Installed, tools: "restic")).IsFailure.ShouldBeFalse();

        Environment.GetEnvironmentVariable("PATH").ShouldBe(_path);
        await _installer.DidNotReceiveWithAnyArgs().Install(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_RehearsesWithTheNodesOwnWhenItWouldInstall()
    {
        (await Ensure(PackageOutcome.WouldInstall, dryRun: true, tools: "tofu")).IsFailure.ShouldBeFalse();

        Environment.GetEnvironmentVariable("PATH").ShouldBe(_path);
    }

    [Fact]
    public async Task Run_FailsAPackageWithoutTheCommand()
    {
        var result = await Ensure(PackageOutcome.Installed, tools: "tofu");

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("has no 'tofu'");
    }

    private static IWorkflowReport Report()
    {
        var report = Substitute.For<IWorkflowReport>();
        report.Section(Arg.Any<string>()).Returns(call => new ReportSection(call.Arg<string>()));
        return report;
    }
}
