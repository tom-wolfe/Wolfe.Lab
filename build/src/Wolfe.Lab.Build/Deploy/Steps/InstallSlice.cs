using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Deploy.Steps;

/// <summary>
/// Puts the slice where the node runs it from.
/// </summary>
[Step("install slice", StepKind.Work)]
internal sealed class InstallSlice(ISliceInstaller installer, IWorkflowLog log)
{
    public async Task<StepResult> Run(Slice slice, CancellationToken ct = default)
    {
        await installer.Install(slice.Source, slice.Release, ct);
        log.Status($"Installed {slice.Name} into {slice.Release.AbsolutePath}.");
        return StepResult.Successful;
    }
}
