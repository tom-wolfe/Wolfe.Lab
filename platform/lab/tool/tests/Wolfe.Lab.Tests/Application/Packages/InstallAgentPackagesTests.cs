using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Packages;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Tests.Application.Packages;

[System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
[Collection(nameof(ProcessPath))]
public class InstallAgentPackagesTests : IDisposable
{
    private readonly DirectoryInfo _package = Directory.CreateTempSubdirectory("lab-alloy-");
    private readonly IPackageInstaller _installer = Substitute.For<IPackageInstaller>();

    private static readonly PackageOptions Alloy = new()
    {
        Github = "grafana/alloy",
        Version = "1.20.1",
        Asset = "alloy-darwin-arm64.zip",
        Checksums = "SHA256SUMS"
    };

    public InstallAgentPackagesTests()
    {
        // Shipped the way Alloy's zip ships it: not executable.
        File.WriteAllText(Program, "");
        File.SetUnixFileMode(Program, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    private readonly string? _path = Environment.GetEnvironmentVariable("PATH");

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("PATH", _path);
        _package.Delete(recursive: true);
    }

    private string Program => Path.Combine(_package.FullName, "alloy-darwin-arm64");

    private Task<StepResult<AgentPackages>> Install(PackageOutcome outcome, bool dryRun = false, AgentOptions? agent = null)
    {
        _installer.Install(Arg.Any<Package>(), Arg.Any<CancellationToken>())
            .Returns(call => new InstalledPackage(call.Arg<Package>(), new PhysicalDirectory(_package.FullName), outcome));
        var declarations = new AgentDeclarations(new Dictionary<string, AgentOptions>
        {
            ["alloy"] = agent ?? new AgentOptions { Package = Alloy, Program = HostPath.From("${PACKAGE}/alloy-darwin-arm64") }
        });
        return new InstallAgentPackages(declarations, _installer, Options.Create(new LabDirectories()),
                new WorkflowJob("agents", "deploy", dryRun, AutoApprove: true), Report(), Substitute.For<IWorkflowLog>())
            .Run(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Run_InstallsEachAgentsPackageByTheAgentsName()
    {
        var packages = (await Install(PackageOutcome.Installed)).Value.ShouldNotBeNull().Packages;

        packages["alloy"].Package.ShouldBe(new Package("alloy", "grafana/alloy", "1.20.1", "v1.20.1", "alloy-darwin-arm64.zip", "SHA256SUMS"));
    }

    [Fact]
    public async Task Run_MakesTheProgramItRunsExecutable()
    {
        await Install(PackageOutcome.Installed);

        (File.GetUnixFileMode(Program) & UnixFileMode.UserExecute).ShouldBe(UnixFileMode.UserExecute);
    }

    [Fact]
    public async Task Run_PutsThePackageFirstOnTheJobsPath()
    {
        await Install(PackageOutcome.Installed);

        Environment.GetEnvironmentVariable("PATH").ShouldBe($"{_package.FullName}{Path.PathSeparator}{_path}");
    }

    [Fact]
    public async Task Run_ChangesNothingOnARehearsal()
    {
        await Install(PackageOutcome.Present, dryRun: true);

        (File.GetUnixFileMode(Program) & UnixFileMode.UserExecute).ShouldBe((UnixFileMode)0);
        Environment.GetEnvironmentVariable("PATH").ShouldBe(_path);
    }

    [Fact]
    public async Task Run_LeavesAnAgentWithoutAPackageToTheNode()
    {
        var packages = (await Install(PackageOutcome.Installed, agent: new AgentOptions { Program = HostPath.From("/bin/sleep") }))
            .Value.ShouldNotBeNull().Packages;

        packages.ShouldBeEmpty();
        await _installer.DidNotReceiveWithAnyArgs().Install(default!, TestContext.Current.CancellationToken);
    }

    private static IWorkflowReport Report()
    {
        var report = Substitute.For<IWorkflowReport>();
        report.Section(Arg.Any<string>()).Returns(call => new ReportSection(call.Arg<string>()));
        return report;
    }
}
