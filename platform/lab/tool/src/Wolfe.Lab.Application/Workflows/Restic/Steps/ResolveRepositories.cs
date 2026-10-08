using Wolfe.Lab.Application.Workflows.Restic.Models;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Backups;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Restic;

namespace Wolfe.Lab.Application.Workflows.Restic.Steps;

/// <summary>
/// Finds the repositories' declaration: what a prune keeps of them, and how much of the offsite
/// copy a check reads back. What it does not declare yet is its <c>ritten.json</c>'s.
/// </summary>
[Step("resolve repositories", StepKind.Work)]
internal sealed class ResolveRepositories(ResticOptions former, IWorkflowLog log)
{
    public StepResult<ResticComponent> Run(DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(WorkflowName.Restic).TryGetValue(out var declared, out var errors))
        {
            return StepResult.Failed(errors);
        }

        var repositories = (ResticComponent)declared;
        if (repositories.Retention is null)
        {
            if (!Retention(former.Retention).TryGetValue(out var retention, out var invalid))
            {
                return StepResult.Failed(invalid);
            }

            repositories.Retention = retention;
        }

        if (repositories.VerifySample is null)
        {
            var share = int.TryParse(former.Verify.ReadDataSubset.TrimEnd('%'), out var percent) ? Percentage.TryFrom(percent) : null;
            if (share is not { IsSuccess: true })
            {
                return new Error($"'verify.readDataSubset' is '{former.Verify.ReadDataSubset}' in ritten.json: a percentage, as restic spells it, such as 5%.");
            }

            repositories.VerifySample = share.ValueObject;
        }

        log.Detail($"Keeps {repositories.Retention.Daily} daily, {repositories.Retention.Weekly} weekly and {repositories.Retention.Monthly} monthly; reads {repositories.VerifySample.Value.Value}% of the offsite copy back.");
        return repositories;
    }

    // The ritten.json's policy, held to the domain's rules.
    private static Result<RetentionPolicy> Retention(RetentionOptions written)
    {
        var counts = new[] { written.Daily, written.Weekly, written.Monthly }.Select(SnapshotCount.TryFrom).ToList();
        if (counts.FirstOrDefault(count => !count.IsSuccess) is { } invalid)
        {
            return new Error($"'retention' in ritten.json: {invalid.Error.ErrorMessage}");
        }

        return RetentionPolicy.Create(counts[0].ValueObject, counts[1].ValueObject, counts[2].ValueObject) is { Value: { } policy }
            ? policy with { KeepTags = written.KeepTags }
            : new Error("'retention' in ritten.json keeps nothing: every snapshot would be pruned.");
    }
}
