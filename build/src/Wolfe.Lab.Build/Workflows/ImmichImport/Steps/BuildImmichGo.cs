using Ritten.Docker;
using Wolfe.Lab.Build.Workflows.ImmichImport.Models;

namespace Wolfe.Lab.Build.Workflows.ImmichImport.Steps;

/// <summary>
/// Builds the import tool's image from the Dockerfile in the component, which pins the release
/// and its checksum.
/// </summary>
[Step("build immich-go", StepKind.Work)]
internal sealed class BuildImmichGo(IDocker docker, IFileSystem fileSystem, IWorkflowLog log)
{
    private const string Tag = "lab/immich-go";
    private const string ContextDirectory = "immich-go";

    public async Task<StepResult<ImmichGoImage>> Run(CancellationToken ct = default)
    {
        var context = fileSystem.ProjectRoot.GetDirectory(ContextDirectory);
        if (!context.GetFile("Dockerfile").Exists)
        {
            return new Error($"{context.AbsolutePath} has no Dockerfile.");
        }

        await docker.Build(context, Tag, ct: ct);
        log.Status($"Built {Tag}.");
        return new ImmichGoImage(Tag, context);
    }
}
