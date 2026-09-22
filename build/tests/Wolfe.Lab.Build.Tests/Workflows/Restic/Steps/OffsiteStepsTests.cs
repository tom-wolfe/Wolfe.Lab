using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Workflows.Restic.Models;
using Wolfe.Lab.Build.Workflows.Restic.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Restic.Steps;

public class OffsiteStepsTests
{
    private static readonly ResticRepository Local = new(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic" });
    private static readonly OffsiteRepository Offsite = new(new ResticRepository(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "s3:https://b2/bucket" }));
    private static readonly RetentionPolicy Policy = new(7, 5, 12, ["pre-upgrade"]);
    private static readonly WorkflowJob Job = new("restic", "offsite");
    private readonly IRestic _restic = Substitute.For<IRestic>();

    [Fact]
    public async Task ApplyRetention_PrunesTheLocalRepositoryThenTheOffsiteOne()
    {
        var result = await new ApplyRetention(_restic, Policy, Job, Substitute.For<IWorkflowLog>()).Run(Local, Offsite, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        Received.InOrder(async () =>
        {
            await _restic.Prune(Local, Policy, Arg.Any<CancellationToken>());
            await _restic.Prune(Offsite.Repository, Policy, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task CopyOffsite_ShipsToTheDestination()
    {
        var result = await new CopyOffsite(_restic, Job, Substitute.For<IWorkflowLog>()).Run(Offsite, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _restic.Received().Copy(Offsite, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckRepositories_ReadsDataBackFromTheOffsiteCopyOnly()
    {
        var result = await new CheckRepositories(_restic, new VerifyOptions("5%"), Substitute.For<IWorkflowLog>()).Run(Local, Offsite, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _restic.Received().Check(Local, null, Arg.Any<CancellationToken>());
        await _restic.Received().Check(Offsite.Repository, "5%", Arg.Any<CancellationToken>());
    }
}
