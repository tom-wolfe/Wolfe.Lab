using Microsoft.Extensions.Options;
using Ritten.Forgejo;
using Ritten.Reporting.Sinks;

namespace Wolfe.Lab.Build.Alerts;

/// <summary>
/// The failure alert every workflow used to carry as an <c>if: failure()</c> step, moved to
/// where the failure is known: the run's result. A job that fails on the runner pages; one that
/// fails at a terminal is already being watched.
/// </summary>
internal sealed class AlertOnFailure(IAlerts alerts, IOptions<ForgejoActionsOptions> run) : IWorkflowResultSink
{
    private WorkflowJob? _job;

    /// <inheritdoc />
    public Task Started(WorkflowJob job, CancellationToken cancellationToken = default)
    {
        _job = job;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task Publish(WorkflowReport report, CancellationToken cancellationToken = default)
    {
        if (report.Succeeded)
        {
            return;
        }

        var title = _job is { } job ? $"{job.Workflow} {job.Name} failed" : $"{report.Title} failed";
        await alerts.Send(new Alert(title, Message(report, run.Value.RunUrl)), cancellationToken);
    }

    /// <summary>
    /// The step and its first error, then the run to open; whichever of them is known.
    /// </summary>
    internal static string Message(WorkflowReport report, string? runUrl)
    {
        var lines = new List<string>();
        if (report.Failure is { } failure)
        {
            var error = failure.Result.Errors?.FirstOrDefault()?.Message;
            lines.Add(error is null ? failure.Step.Name : $"{failure.Step.Name}: {error}");
        }

        if (runUrl is { } url)
        {
            lines.Add(url);
        }

        return string.Join('\n', lines);
    }
}
