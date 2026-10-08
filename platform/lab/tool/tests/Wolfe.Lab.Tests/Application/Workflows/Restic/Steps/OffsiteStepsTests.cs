using Wolfe.Lab.Application.Workflows.Restic.Steps;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Backups;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Restic;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Restic;

namespace Wolfe.Lab.Tests.Application.Workflows.Restic.Steps;

public class OffsiteStepsTests
{
    private static readonly ResticRepository Local = new(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic" });
    private static readonly OffsiteRepository Offsite = new(new ResticRepository(new Dictionary<string, string> { ["RESTIC_REPOSITORY"] = "s3:https://b2/bucket" }));
    private static readonly RetentionPolicy Policy =
        RetentionPolicy.Create(SnapshotCount.From(7), SnapshotCount.From(5), SnapshotCount.From(12)).Value.ShouldNotBeNull() with { KeepTags = ["pre-upgrade"] };

    private static readonly ResticComponent Repositories = Declared();

    private static ResticComponent Declared()
    {
        var repositories = ResticComponent.Create(new DocumentSource(RepositoryPath.From("platform/restic/repositories/component.yaml")),
            ComponentName.From("repositories"), ComponentKind.Repository, Policy).Value.ShouldNotBeNull();
        repositories.VerifySample = Percentage.From(5);
        return repositories;
    }
    private static readonly WorkflowJob Job = new("restic", "offsite");
    private readonly IRestic _restic = Substitute.For<IRestic>();

    [Fact]
    public async Task ApplyRetention_PrunesTheLocalRepositoryThenTheOffsiteOne()
    {
        var result = await new ApplyRetention(_restic, Job, Substitute.For<IWorkflowLog>()).Run(Repositories, Local, Offsite, TestContext.Current.CancellationToken);

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
        var result = await new CheckRepositories(_restic, Substitute.For<IWorkflowLog>()).Run(Repositories, Local, Offsite, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _restic.Received().Check(Local, null, Arg.Any<CancellationToken>());
        await _restic.Received().Check(Offsite.Repository, Percentage.From(5), Arg.Any<CancellationToken>());
    }
}
