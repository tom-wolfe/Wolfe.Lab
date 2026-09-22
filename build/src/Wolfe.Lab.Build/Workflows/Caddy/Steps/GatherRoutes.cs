using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Slices;

namespace Wolfe.Lab.Build.Workflows.Caddy.Steps;

/// <summary>
/// Puts every slice's route snippet where the front door reads them from.
/// </summary>
/// <remarks>
/// The one step in the lab that reads other slices, and deliberately: the Caddyfile's import
/// glob is the front door's contract, and gathering from the CHECKOUT rather than trusting the
/// install root is what makes this deploy independent of every other slice's. A route added in
/// the same push as its slice would otherwise depend on which of the two workflows the runner
/// picked up first.
/// </remarks>
[Step("gather routes", StepKind.Work)]
internal sealed class GatherRoutes(WorkflowJob job, IWorkflowLog log)
{
    internal const string Snippet = "caddy.caddyfile";

    public StepResult Run(Slice slice)
    {
        var checkout = Parent(slice.Source);
        var installs = Parent(slice.Release);

        var routes = Directory.GetDirectories(checkout)
            .Select(directory => (Slice: Path.GetFileName(directory), File: Path.Combine(directory, Snippet)))
            .Where(candidate => File.Exists(candidate.File))
            .OrderBy(candidate => candidate.Slice, StringComparer.Ordinal)
            .ToList();

        foreach (var (name, file) in routes)
        {
            var target = new PhysicalDirectory(Path.Combine(installs, name));
            if (job.DryRun)
            {
                continue;
            }

            target.Create();
            File.Copy(file, Path.Combine(target.AbsolutePath, Snippet), overwrite: true);
        }

        var names = string.Join(", ", routes.Select(route => route.Slice));
        if (job.DryRun)
        {
            log.Skipped($"Would gather {routes.Count} route{(routes.Count == 1 ? "" : "s")}: {names}.");
        }
        else
        {
            log.Status($"Gathered {routes.Count} route{(routes.Count == 1 ? "" : "s")}: {names}.");
        }

        return StepResult.Successful;
    }

    private static string Parent(IDirectory directory) =>
        Path.GetDirectoryName(directory.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar)) ?? directory.AbsolutePath;
}
