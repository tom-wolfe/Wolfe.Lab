using Ritten.Git;
using Wolfe.Lab.Build.Workflows.CaddyRoutes.Models;

namespace Wolfe.Lab.Build.Workflows.CaddyRoutes.Steps;

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

        // Hidden directories are skipped by default, which keeps .git out of the walk.
        var snippets = Directory.EnumerateFiles(checkout.AbsolutePath, Snippet, new EnumerationOptions { RecurseSubdirectories = true })
            .Select(snippet => (Snippet: snippet, Name: Name(checkout, snippet)))
            .OrderBy(route => route.Name, StringComparer.Ordinal);

        var names = new List<string>();
        foreach (var (snippet, name) in snippets)
        {
            File.Copy(snippet, Path.Combine(staging.AbsolutePath, name + Extension), overwrite: true);
            names.Add(name);
        }

        log.Status($"Gathered {names.Count} route{(names.Count == 1 ? "" : "s")}: {string.Join(", ", names)}.");
        return new StagedRoutes(staging, names);
    }

    private static string Name(IDirectory checkout, string snippet) =>
        Path.GetRelativePath(checkout.AbsolutePath, Path.GetDirectoryName(snippet) ?? checkout.AbsolutePath).Replace(Path.DirectorySeparatorChar, '-');
}
