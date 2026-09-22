using Wolfe.Lab.Build.Workflows.Chezmoi.Models;

namespace Wolfe.Lab.Build.Workflows.Chezmoi.Steps;

/// <summary>
/// Runs shellcheck over the rendered scripts.
/// </summary>
/// <remarks>
/// Only the output is shell: everything in the source is a Go template first, so this is the
/// earliest point either can be checked at all.
/// </remarks>
[Step("check scripts", StepKind.Check)]
internal sealed class CheckScripts(ICommandRunner commands, IWorkflowLog log)
{
    internal const string ScriptsDirectory = ".chezmoiscripts";

    public async Task<StepResult> Run(RenderedProfiles rendered, CancellationToken ct = default)
    {
        var scripts = rendered.Profiles
            .SelectMany(profile => profile.Files
                .Where(file => file.StartsWith(ScriptsDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Select(file => Path.Combine(profile.Directory.AbsolutePath, file)))
            .ToList();
        if (scripts.Count == 0)
        {
            log.Detail("No scripts rendered.");
            return StepResult.Successful;
        }

        var result = await commands.Run(Command.Create("shellcheck").WithArguments([.. scripts]), ct);
        if (result.IsError)
        {
            return new Error($"shellcheck found problems in the rendered scripts:\n{result.StandardOutput}");
        }

        log.Status($"{scripts.Count} rendered script{(scripts.Count == 1 ? "" : "s")} passed shellcheck.");
        return StepResult.Successful;
    }
}
