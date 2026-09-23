namespace Wolfe.Lab.Build.Clients.Releases.Steps;

/// <summary>
/// Puts the component where the node runs it from.
/// </summary>
[Step("install release", StepKind.Work)]
internal sealed class InstallRelease(IReleaseInstaller installer, IFileSystem fileSystem, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(Release release, CancellationToken ct = default)
    {
        await installer.Install(fileSystem.ProjectRoot, release.Directory, ct);
        if (!job.DryRun)
        {
            // The rehearsal has already itemised what it would change.
            log.Status($"Installed {release.Name} into {release.Directory.AbsolutePath}.");
        }

        return StepResult.Successful;
    }
}
