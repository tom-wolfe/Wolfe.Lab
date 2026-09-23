using Wolfe.Lab.Build.Workflows.ImmichImport.Models;

namespace Wolfe.Lab.Build.Workflows.ImmichImport.Steps;

/// <summary>
/// Finds the Takeout's parts before anything is built or started for them.
/// </summary>
[Step("resolve takeout", StepKind.Check)]
internal sealed class ResolveTakeout(TakeoutLocation location, IWorkflowLog log)
{
    public StepResult<Takeout> Run()
    {
        var directory = location.Directory;
        if (!directory.Exists)
        {
            return new Error($"{directory.AbsolutePath} does not exist.");
        }

        var parts = directory.GetFiles("*.zip").Select(f => f.Name).Order(StringComparer.Ordinal).ToList();
        if (parts.Count == 0)
        {
            return new Error($"{directory.AbsolutePath} holds no zip parts.");
        }

        log.Detail($"{parts.Count} Takeout part{(parts.Count == 1 ? "" : "s")} in {directory.AbsolutePath}.");
        return new Takeout(directory, parts);
    }
}
