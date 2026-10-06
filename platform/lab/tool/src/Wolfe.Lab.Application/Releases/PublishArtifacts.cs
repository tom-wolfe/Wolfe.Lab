using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Releases;

/// <summary>
/// Mirrors each artifact onto the node, and reads back when their content last changed there.
/// </summary>
[Step("publish artifacts", StepKind.Work)]
internal sealed class PublishArtifacts(IReleaseInstaller installer, IFileSystem fileSystem, IWorkflowReport report, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult<PublishedArtifacts>> Run(Artifacts artifacts, CancellationToken ct = default)
    {
        foreach (var artifact in artifacts.Items)
        {
            var changes = await installer.Install(artifact.Source, artifact.Output, ct);
            Report(artifact, changes);
            if (!job.DryRun)
            {
                // The rehearsal has already itemised what it would change.
                log.Status(changes.Count == 0
                    ? $"{artifact.Output.AbsolutePath} already matched."
                    : $"Published {artifact.Output.AbsolutePath}: {Files(changes.Count)} changed.");
            }
        }

        return new PublishedArtifacts(artifacts.Items, Stamp(artifacts.Items));
    }

    /// <summary>
    /// One line per artifact in the run's report — what was published where, and how much of it
    /// changed — with the files themselves folded away beneath it.
    /// </summary>
    private void Report(Artifact artifact, IReadOnlyList<string> changes)
    {
        var section = report.Section(ReportSections.Artifacts);
        var relative = fileSystem.ProjectRoot.RelativePath(artifact.Source);
        var source = relative == "." ? "the component" : $"`{relative}`";
        var output = $"`{artifact.Output.AbsolutePath}`";
        if (changes.Count == 0)
        {
            section.Note(job.DryRun ? $"Would publish {source} to {output}, which already matches." : $"Published {source} to {output}, which already matched.");
            return;
        }

        var summary = job.DryRun ? $"Would publish {source} to {output}: {Files(changes.Count)} to change." : $"Published {source} to {output}: {Files(changes.Count)} changed.";
        if (job.DryRun)
        {
            section.Note(summary);
        }
        else
        {
            section.Success(summary);
        }

        section.Details($"Files in {output}", $"```\n{string.Join('\n', changes)}\n```");
    }

    private static string Files(int count) => count == 1 ? "1 file" : $"{count} files";

    /// <summary>
    /// The newest write among the outputs' files and directories — a directory's time moves when
    /// an entry is added, removed or replaced, so a deleted file counts as a change too.
    /// </summary>
    internal static DateTimeOffset? Stamp(IEnumerable<Artifact> artifacts)
    {
        DateTime? newest = null;
        foreach (var output in artifacts.Select(artifact => artifact.Output.AbsolutePath).Where(Directory.Exists))
        {
            var entries = Directory.EnumerateFileSystemEntries(output, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 })
                .Append(output);
            foreach (var entry in entries)
            {
                var written = Directory.Exists(entry) ? Directory.GetLastWriteTimeUtc(entry) : File.GetLastWriteTimeUtc(entry);
                if (newest is null || written > newest)
                {
                    newest = written;
                }
            }
        }

        return newest is { } stamp ? new DateTimeOffset(stamp, TimeSpan.Zero) : null;
    }
}
