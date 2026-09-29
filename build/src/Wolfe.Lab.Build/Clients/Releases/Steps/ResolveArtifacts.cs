using Ritten.Engine.FileSystem;

namespace Wolfe.Lab.Build.Clients.Releases.Steps;

/// <summary>
/// Turns the declared artifacts into directories on this node, refusing any it must not write.
/// </summary>
/// <remarks>
/// An output is mirrored — what its source does not have is deleted — so it must lie strictly
/// inside <c>${LAB_ROOT}</c>, where everything is the lab's. A typo pointing one at a home
/// directory is a refusal here, not a deletion there.
/// </remarks>
[Step("resolve artifacts", StepKind.Work)]
internal sealed class ResolveArtifacts(ArtifactDeclarations declarations, IFileSystem fileSystem, WorkflowEnvironment environment, IWorkflowLog log)
{
    public StepResult<Artifacts> Run()
    {
        var roots = LabRoots.From(environment);
        var component = fileSystem.ProjectRoot.AbsolutePath;
        var resolved = new List<Artifact>();
        var errors = new List<Error>();

        foreach (var artifact in declarations.Artifacts)
        {
            if (artifact.Source is not { Length: > 0 } source || artifact.Output is not { Length: > 0 } output)
            {
                errors.Add(new Error("Every entry in 'artifacts' needs a 'source' and an 'output'."));
                continue;
            }

            var from = Path.GetFullPath(Path.Combine(component, source));
            if (from != Path.GetFullPath(component) && !Inside(from, component))
            {
                errors.Add(new Error($"Artifact '{source}' is outside the component; publish only what the component holds."));
                continue;
            }

            if (!Directory.Exists(from))
            {
                errors.Add(new Error($"Artifact '{source}' is not a directory of the component."));
                continue;
            }

            var to = roots.Expand(output);
            if (!Path.IsPathFullyQualified(to) || !roots.Contains(to))
            {
                errors.Add(new Error($"Artifact '{source}' publishes to '{to}', which is not inside {roots.Root} (${{{LabRoots.RootVariable}}}): an output is mirrored, so it must be the lab's alone."));
                continue;
            }

            resolved.Add(new Artifact(new PhysicalDirectory(from), new PhysicalDirectory(Path.GetFullPath(to))));
        }

        foreach (var (outer, inner) in resolved.SelectMany(a => resolved.Where(b => !ReferenceEquals(a, b)).Select(b => (a, b))))
        {
            if (Inside(inner.Output.AbsolutePath, outer.Output.AbsolutePath) || inner.Output.AbsolutePath == outer.Output.AbsolutePath)
            {
                errors.Add(new Error($"Artifacts publish to {outer.Output.AbsolutePath} and {inner.Output.AbsolutePath}: one would mirror the other away."));
            }
        }

        if (errors.Count > 0)
        {
            return StepResult.Failed(errors.DistinctBy(error => error.Message));
        }

        foreach (var artifact in resolved)
        {
            log.Detail($"{artifact.Source.AbsolutePath} publishes to {artifact.Output.AbsolutePath}.");
        }

        return new Artifacts(resolved);
    }

    private static bool Inside(string path, string directory) =>
        Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar, StringComparison.Ordinal);
}
