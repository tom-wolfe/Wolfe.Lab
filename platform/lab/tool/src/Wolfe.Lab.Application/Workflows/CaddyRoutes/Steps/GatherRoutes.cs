using Ritten.Git;
using Wolfe.Lab.Application.Workflows.CaddyRoutes.Models;

namespace Wolfe.Lab.Application.Workflows.CaddyRoutes.Steps;

/// <summary>
/// Collects every component's route snippet from the checkout into a staging directory.
/// </summary>
[Step("gather routes", StepKind.Work)]
internal sealed class GatherRoutes(IFileSystem fileSystem, IGit git, IWorkflowLog log)
{
    internal const string Snippet = "caddy.caddyfile";
    internal const string Extension = ".caddyfile";

    public async Task<StepResult<StagedRoutes>> Run(CancellationToken ct = default)
    {
        if (await git.RepositoryRoot(ct) is not { } checkout)
        {
            return new Error($"{fileSystem.ProjectRoot.AbsolutePath} is not in a git checkout, and the routes are gathered from one.");
        }

        var staging = fileSystem.Temp.GetDirectory("routes");
        if (staging.Exists)
        {
            staging.Delete();
        }

        staging.Create();

        // Hidden directories are skipped, which keeps .git — and any worktree under a dot — out of the walk.
        var snippets = checkout.GetFiles($"**/{Snippet}")
            .Where(snippet => !checkout.RelativePath(snippet).Split('/').Any(segment => segment.StartsWith('.')))
            .Select(snippet => (Snippet: snippet, Name: Name(checkout, snippet)))
            .OrderBy(route => route.Name, StringComparer.Ordinal);

        var names = new List<string>();
        foreach (var (snippet, name) in snippets)
        {
            await using (var from = snippet.OpenRead())
            await using (var to = staging.GetFile(name + Extension).OpenWrite())
            {
                await from.CopyToAsync(to, ct);
            }

            names.Add(name);
        }

        log.Status($"Gathered {names.Count} route{(names.Count == 1 ? "" : "s")}: {string.Join(", ", names)}.");
        return new StagedRoutes(staging, names);
    }

    private static string Name(IDirectory checkout, IFile snippet) => checkout.RelativePath(snippet.Directory).Replace('/', '-');
}
