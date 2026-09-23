using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Clients.Chezmoi;
using Wolfe.Lab.Build.Workflows.Chezmoi.Models;

namespace Wolfe.Lab.Build.Workflows.Chezmoi.Steps;

/// <summary>
/// Renders the whole source for every profile, so a template that only breaks on one machine
/// fails here instead of on that machine's next update.
/// </summary>
/// <remarks>
/// The source handed to chezmoi is the checkout, not the slice: the checkout's
/// <c>.chezmoiroot</c> names <c>chezmoi/home</c>, which is how chezmoi itself finds the tree on a
/// node. The listing is the review aid — what each machine gets — and goes to the detailed log.
/// </remarks>
[Step("render profiles", StepKind.Check)]
internal sealed class RenderProfiles(IChezmoi chezmoi, Profiles profiles, IFileSystem fileSystem, IWorkflowLog log)
{
    public async Task<StepResult<RenderedProfiles>> Run(CancellationToken ct = default)
    {
        // The checkout, two levels up from the component: chezmoi's own .chezmoiroot lives there
        // and names the source tree, so the renderer reads the repository exactly as a node does.
        var source = new PhysicalDirectory(Path.GetFullPath(Path.Combine(fileSystem.ProjectRoot.AbsolutePath, "..", "..")));
        var scratch = new PhysicalDirectory(Directory.CreateTempSubdirectory("lab-render-").FullName);
        var rendered = new List<RenderedProfile>();
        foreach (var profile in profiles.Names)
        {
            var destination = scratch.GetDirectory(profile);
            var files = await chezmoi.Render(source, profile, destination, ct);
            log.Status($"{profile}: {files.Count} file{(files.Count == 1 ? "" : "s")}.");
            foreach (var file in files)
            {
                log.Detail($"  {file}");
            }

            rendered.Add(new RenderedProfile(profile, destination, files));
        }

        return new RenderedProfiles(rendered);
    }
}
