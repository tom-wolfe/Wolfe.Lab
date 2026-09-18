using Ritten.Docker;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Immich.Models;

namespace Wolfe.Lab.Build.Immich.Steps;

/// <summary>
/// Builds the import tool's image from the Dockerfile beside the slice, which pins the release
/// and its checksum.
/// </summary>
[Step("build immich-go", StepKind.Work)]
internal sealed class BuildImmichGo(IDocker docker, IWorkflowLog log)
{
    private const string Tag = "lab/immich-go";
    private const string ContextDirectory = "immich-go";

    public async Task<StepResult<ImmichGoImage>> Run(Slice slice, CancellationToken ct = default)
    {
        var context = slice.Source.GetDirectory(ContextDirectory);
        if (!context.GetFile("Dockerfile").Exists)
        {
            return new Error($"{context.AbsolutePath} has no Dockerfile.");
        }

        await docker.Build(context, Tag, ct: ct);
        log.Status($"Built {Tag}.");
        return new ImmichGoImage(Tag, context);
    }
}
