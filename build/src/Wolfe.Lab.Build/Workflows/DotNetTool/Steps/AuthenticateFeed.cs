using Microsoft.Extensions.Options;
using Ritten.NuGet;
using Wolfe.Lab.Build.Workflows.DotNetTool.Models;

namespace Wolfe.Lab.Build.Workflows.DotNetTool.Steps;

/// <summary>
/// The feed <see cref="Ritten.NuGet.Steps.NugetPush"/> publishes to, with its key read from the vault.
/// </summary>
/// <remarks>
/// The lab's stand-in for Ritten's <c>NugetAuthenticate</c>, which takes the key from the environment or asks at a
/// terminal: a job here has neither, only a reference, resolved at the last moment like every other secret.
/// </remarks>
[Step("authenticate feed", StepKind.Work)]
internal sealed class AuthenticateFeed(IOptions<NuGetOptions> options, ISecretProvider secrets, FeedToken token, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult<NuGetFeed>> Run(CancellationToken ct = default)
    {
        var feed = new NuGetFeed(options.Value.Feed);
        if (job.DryRun)
        {
            log.Skipped("No key needed: this is a dry run.");
            return feed;
        }

        return feed.WithApiKey(await secrets.Resolve(token.Reference.Value, ct));
    }
}
