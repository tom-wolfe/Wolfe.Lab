using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Backup.Steps;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Restic;

namespace Wolfe.Lab.Build.Tests.Backup.Steps;

public class ResolveSnapshotTests
{
    private static readonly ResticRepository Repository = new(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic" });
    private static readonly ResticSnapshot Latest = new("0ff3ec5c", DateTimeOffset.Parse("2026-09-18T02:20:00Z"), ["service:forgejo", "image:codeberg.org/forgejo/forgejo:13"], ["/Users/lab/Docker/forgejo/data"]);
    private readonly IRestic _restic = Substitute.For<IRestic>();
    private readonly Slice _slice = new("forgejo", new PhysicalDirectory("/lab/forgejo"), new PhysicalDirectory("/lab/release/forgejo"));

    private ResolveSnapshot Step(string? id) => new(_restic, new RestoreRequest(id, false), Substitute.For<IWorkflowLog>());

    [Fact]
    public async Task Run_TakesTheSliceLatestByDefault()
    {
        _restic.FindSnapshot(Repository, "service:forgejo", null, Arg.Any<CancellationToken>()).Returns(Latest);

        var result = await Step(null).Run(_slice, Repository, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Snapshot.ShouldBe(Latest);
        Latest.Image.ShouldBe("codeberg.org/forgejo/forgejo:13");
    }

    [Fact]
    public async Task Run_AsksForTheSnapshotNamed()
    {
        _restic.FindSnapshot(Repository, "service:forgejo", "5c27134a", Arg.Any<CancellationToken>()).Returns(Latest with { Id = "5c27134a" });

        var result = await Step("5c27134a").Run(_slice, Repository, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Snapshot.Id.ShouldBe("5c27134a");
    }

    [Fact]
    public async Task Run_FailsWhenNothingMatches()
    {
        _restic.FindSnapshot(Repository, "service:forgejo", null, Arg.Any<CancellationToken>()).Returns((ResticSnapshot?)null);

        var result = await Step(null).Run(_slice, Repository, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
    }
}
