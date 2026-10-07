using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Releases;

/// <summary>
/// Mirrors the deployment's directory onto the node, under its name, and reads back when its
/// content last changed there.
/// </summary>
/// <remarks>
/// A runner checks the repository out into a disposable workspace, and what runs goes on reading
/// its files — config directories, a Caddyfile, an agent's config — long after the job is gone.
/// The install is a mirror, so what the deployment no longer has is deleted from it.
/// </remarks>
[Step("install deployment", StepKind.Work)]
internal sealed class InstallDeployment(IReleaseInstaller installer, IFileSystem fileSystem, IOptions<LabDirectories> options, IWorkflowReport report, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult<Installation>> Run(DeploymentUnit unit, CancellationToken ct = default)
    {
        var installed = options.Value.DeployedTo(unit);
        var changes = await installer.Install(fileSystem.ProjectRoot, installed, ct);
        Report(unit, installed, changes);
        if (!job.DryRun)
        {
            // The rehearsal has already itemised what it would change.
            log.Status(changes.Count == 0
                ? $"{installed.AbsolutePath} already matched."
                : $"Installed {unit.Name} into {installed.AbsolutePath}: {Files(changes.Count)} changed.");
        }

        return new Installation(installed, Stamp(installed));
    }

    /// <summary>
    /// One line in the run's report — where the deployment went, and how much of it changed —
    /// with the files themselves folded away beneath it.
    /// </summary>
    private void Report(DeploymentUnit unit, IDirectory installed, IReadOnlyList<string> changes)
    {
        var section = report.Section(ReportSections.Install);
        var output = $"`{installed.AbsolutePath}`";
        if (changes.Count == 0)
        {
            section.Note(job.DryRun ? $"Would install {unit.Name} into {output}, which already matches." : $"Installed {unit.Name} into {output}, which already matched.");
            return;
        }

        var summary = job.DryRun ? $"Would install {unit.Name} into {output}: {Files(changes.Count)} to change." : $"Installed {unit.Name} into {output}: {Files(changes.Count)} changed.";
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
    /// The newest write among the install's files and directories — a directory's time moves when
    /// an entry is added, removed or replaced, so a deleted file counts as a change too.
    /// </summary>
    internal static DateTimeOffset? Stamp(IDirectory installed) =>
        installed.Exists
            ? installed.GetDirectories(recursive: true).Select(directory => directory.LastWriteTime)
                .Concat(installed.GetFiles("**/*").Select(file => file.LastWriteTime))
                .Append(installed.LastWriteTime)
                .Max()
            : null;
}
