using Microsoft.Extensions.Options;
using Ritten.Forgejo;
using Ritten.Reporting.Sinks;

namespace Wolfe.Lab.Build.Clients.Alerts;

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
            lines.Add(error is null ? failure.Step.Name : $"{failure.Step.Name}: {Gist(error)}");
        }

        if (runUrl is { } url)
        {
            lines.Add(url);
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// A phone notification, not a log: Pushover refuses more than 1024 characters, and a tool
    /// that dumps its usage before the actual error puts that error on the last line. So: the
    /// first line, the last line when there is one, each cut to fit.
    /// </summary>
    internal static string Gist(string error)
    {
        const int width = 300;
        var lines = error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            return "";
        }

        var first = Cut(lines[0], width);
        return lines.Length == 1 ? first : $"{first} … {Cut(lines[^1], width)}";
    }

    private static string Cut(string text, int width) => text.Length <= width ? text : text[..(width - 1)] + "…";
}
