using Wolfe.Lab.Build.Immich.Models;

namespace Wolfe.Lab.Build.Immich.Steps;

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

        var parts = directory.GetFiles("*.zip").Count();
        if (parts == 0)
        {
            return new Error($"{directory.AbsolutePath} holds no zip parts.");
        }

        log.Detail($"{parts} Takeout part{(parts == 1 ? "" : "s")} in {directory.AbsolutePath}.");
        return new Takeout(directory, parts);
    }
}
