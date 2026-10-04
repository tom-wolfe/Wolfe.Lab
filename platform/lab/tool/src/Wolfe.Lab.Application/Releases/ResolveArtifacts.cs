using Ritten.Engine.FileSystem;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Releases;

/// <summary>
/// Turns the declared artifacts into directories on this node, refusing any it must not write.
/// </summary>
/// <remarks>
/// An output is mirrored — what its source does not have is deleted — so it must lie strictly
/// inside <c>${LAB_ROOT}</c>, where everything is the lab's. A typo pointing one at a home
/// directory is a refusal here, not a deletion there.
/// </remarks>
[Step("resolve artifacts", StepKind.Work)]
internal sealed class ResolveArtifacts(ArtifactDeclarations declarations, IFileSystem fileSystem, IOptions<LabDirectories> options, IWorkflowLog log)
{
    public StepResult<Artifacts> Run()
    {
        var roots = options.Value;
        var component = fileSystem.ProjectRoot;
        var resolved = new List<Artifact>();
        var errors = new List<Error>();

        foreach (var artifact in declarations.Artifacts)
        {
            if (artifact.Source is not { Length: > 0 } source || artifact.Output is not { Length: > 0 } output)
            {
                errors.Add(new Error("Every entry in 'artifacts' needs a 'source' and an 'output'."));
                continue;
            }

            var from = component.GetDirectory(source);
            if (from.AbsolutePath != component.AbsolutePath && !Inside(from, component))
            {
                errors.Add(new Error($"Artifact '{source}' is outside the component; publish only what the component holds."));
                continue;
            }

            if (!from.Exists)
            {
                errors.Add(new Error($"Artifact '{source}' is not a directory of the component."));
                continue;
            }

            var to = roots.Expand(output);
            if (!Path.IsPathFullyQualified(to) || new PhysicalDirectory(to) is var into && !roots.Contains(into))
            {
                errors.Add(new Error($"Artifact '{source}' publishes to '{to}', which is not inside {roots.Root.AbsolutePath} (${{{LabDirectories.RootVariable}}}): an output is mirrored, so it must be the lab's alone."));
                continue;
            }

            resolved.Add(new Artifact(from, into));
        }

        foreach (var (outer, inner) in resolved.SelectMany(a => resolved.Where(b => !ReferenceEquals(a, b)).Select(b => (a, b))))
        {
            if (Inside(inner.Output, outer.Output) || inner.Output.AbsolutePath == outer.Output.AbsolutePath)
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

    private static bool Inside(IDirectory directory, IDirectory outer) =>
        outer.RelativePath(directory) is var relative && relative != "." && relative != ".." && !relative.StartsWith("../", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
}
