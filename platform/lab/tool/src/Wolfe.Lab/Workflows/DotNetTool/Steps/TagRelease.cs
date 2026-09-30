using Microsoft.Extensions.Options;
using Ritten.DotNet;
using Ritten.Git;

namespace Wolfe.Lab.Workflows.DotNetTool.Steps;

/// <summary>
/// Tags the commit a CLI is published from, <c>lab/v1.0.214</c>, so every version on the feed
/// has a commit to go back to.
/// </summary>
/// <remarks>
/// Ritten's own <c>GitTag</c>, which lives in the <c>Ritten</c> tool package and so cannot be
/// referenced; once it moves into <c>Ritten.Git</c>, this is a <c>using</c> again. A tag already on
/// the remote — a rerun after a failed push — is left alone.
/// </remarks>
[Step("tag release", StepKind.Publish)]
internal sealed class TagRelease(IGit git, IOptions<GitOptions> options, IWorkflowLog log)
{
    public async Task<StepResult> Run(Project project, CancellationToken ct = default)
    {
        var tag = $"{options.Value.TagPrefix}{project.Version}";
        if (await git.RemoteTagExists("origin", tag, ct))
        {
            log.Skipped($"{tag} is already on origin.");
            return StepResult.Successful;
        }

        if (!await git.TagExists(tag, ct))
        {
            await git.CreateTag(tag, options.Value.CommitSha, ct);
        }

        await git.PushTag("origin", tag, ct);
        log.Status($"Tagged {tag}.");
        return StepResult.Successful;
    }
}
