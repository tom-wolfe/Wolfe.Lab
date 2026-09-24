using Wolfe.Lab.Build.Workflows.CaddyRoutes.Models;

namespace Wolfe.Lab.Build.Workflows.CaddyRoutes.Steps;

/// <summary>
/// Collects every component's route snippet from the checkout into a staging directory.
/// </summary>
/// <remarks>
/// The one step in the lab that reads other slices, and deliberately: the Caddyfile's import
/// glob is the front door's contract, and gathering from the CHECKOUT rather than trusting the
/// install root is what makes this deploy independent of every other component's. A route added
/// in the same push as its stack would otherwise depend on which of the two workflows the
/// runner picked up first. A snippet is filed as <c>&lt;slice&gt;-&lt;component&gt;.caddyfile</c>,
/// so two components of one slice can each publish a name.
/// </remarks>
[Step("gather routes", StepKind.Work)]
internal sealed class GatherRoutes(IFileSystem fileSystem, IWorkflowLog log)
{
    internal const string Snippet = "caddy.caddyfile";
    internal const string Extension = ".caddyfile";

    public StepResult<StagedRoutes> Run()
    {
        var checkout = Checkout(fileSystem.ProjectRoot);
        var staging = fileSystem.Temp.GetDirectory("routes");
        if (staging.Exists)
        {
            staging.Delete();
        }

        staging.Create();

        var names = new List<string>();
        foreach (var slice in Directory.GetDirectories(checkout).OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            foreach (var component in Directory.GetDirectories(slice).OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                var snippet = Path.Combine(component, Snippet);
                if (!File.Exists(snippet))
                {
                    continue;
                }

                var name = $"{Path.GetFileName(slice)}-{Path.GetFileName(component)}";
                File.Copy(snippet, Path.Combine(staging.AbsolutePath, name + Extension), overwrite: true);
                names.Add(name);
            }
        }

        log.Status($"Gathered {names.Count} route{(names.Count == 1 ? "" : "s")}: {string.Join(", ", names)}.");
        return new StagedRoutes(staging, names);
    }

    /// <summary>
    /// The checkout is two levels up: the component sits in its slice, the slice in the repository.
    /// </summary>
    private static string Checkout(IDirectory component) =>
        Path.GetFullPath(Path.Combine(component.AbsolutePath, "..", ".."));
}
