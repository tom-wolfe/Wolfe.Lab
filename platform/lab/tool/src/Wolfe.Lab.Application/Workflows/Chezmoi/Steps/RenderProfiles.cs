using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Application.Workflows.Chezmoi.Models;
using Wolfe.Lab.Infrastructure.Chezmoi;

namespace Wolfe.Lab.Application.Workflows.Chezmoi.Steps;

/// <summary>
/// Renders the whole source for every profile, so a template that only breaks on one machine
/// fails here instead of on that machine's next update.
/// </summary>
/// <remarks>
/// The source handed to chezmoi is the checkout, not the service: the checkout's
/// <c>.chezmoiroot</c> names <c>platform/chezmoi/home</c>, which is how chezmoi itself finds the tree on a
/// node. The listing is the review aid — what each machine gets — and goes to the detailed log.
/// </remarks>
[Step("render profiles", StepKind.Check)]
internal sealed class RenderProfiles(IChezmoi chezmoi, Profiles profiles, IFileSystem fileSystem, IGit git, IWorkflowLog log)
{
    public async Task<StepResult<RenderedProfiles>> Run(CancellationToken ct = default)
    {
        if (await git.RepositoryRoot(ct) is not { } source)
        {
            return new Error($"{fileSystem.ProjectRoot.AbsolutePath} is not in a git checkout, and the profiles are rendered from one.");
        }

        var scratch = fileSystem.CreateTempDirectory("lab-render-");
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
