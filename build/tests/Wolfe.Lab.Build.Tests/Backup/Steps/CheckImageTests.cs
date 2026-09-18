using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Backup.Steps;

namespace Wolfe.Lab.Build.Tests.Backup.Steps;

public class CheckImageTests
{
    private static RestorePoint Taken(string? image) =>
        new(new ResticSnapshot("0ff3ec5c", DateTimeOffset.UnixEpoch, image is null ? ["service:forgejo"] : ["service:forgejo", $"image:{image}"], []));

    private static CheckImage Step(bool anyImage = false) => new(new RestoreRequest(null, anyImage), Substitute.For<IWorkflowLog>());

    [Fact]
    public void Run_PassesWhenTheStackRunsTheImageTheSnapshotWasTakenUnder() =>
        Step().Run(Taken("forgejo:13"), new SnapshotImage("forgejo:13")).IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_RefusesADifferentImage()
    {
        var result = Step().Run(Taken("forgejo:13"), new SnapshotImage("forgejo:14"));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("--any-image");
    }

    [Fact]
    public void Run_PassesADifferentImageWhenAsked() =>
        Step(anyImage: true).Run(Taken("forgejo:13"), new SnapshotImage("forgejo:14")).IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_HasNothingToHoldAWarmSliceTo() =>
        Step().Run(Taken(null), SnapshotImage.None).IsFailure.ShouldBeFalse();
}
