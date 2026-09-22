using System.Globalization;
using Ritten.Docker;
using Wolfe.Lab.Build.Workflows.Immich.Models;

namespace Wolfe.Lab.Build.Workflows.Immich.Steps;

/// <summary>
/// Runs immich-go against the Takeout, on the lab network so the server's container name
/// resolves. The server and key travel as immich-go's own environment variables, so the key
/// is never an argument on the host. Server jobs pause for the duration and the session is
/// tagged, so what one run imported can be told apart afterwards.
/// </summary>
[Step("import takeout", StepKind.Publish)]
internal sealed class ImportTakeout(IDocker docker, ISecretProvider secrets, ImmichServer server, ImportOptions options, IWorkflowLog log)
{
    internal const string Network = "lab";
    internal const string MountPoint = "/takeout";

    public async Task<StepResult> Run(Takeout takeout, ImmichGoImage image, CancellationToken ct = default)
    {
        var key = await secrets.Resolve(server.ApiKey.Value, ct);
        var run = new ContainerRun(
            image.Tag,
            [
                "upload", "from-google-photos",
                "--concurrent-tasks", options.Concurrency.ToString(CultureInfo.InvariantCulture),
                "--pause-immich-jobs",
                "--session-tag",
                "--include-unmatched",
                // A runner log, not a terminal: the progress screen would be noise.
                "--no-ui",
                // The archives by name: docker run globs nothing, and a directory means an
                // extracted Takeout to immich-go.
                .. takeout.Parts.Select(part => $"{MountPoint}/{part}")
            ])
        {
            Mounts = [new BindMount(takeout.Directory, MountPoint, ReadOnly: true)],
            Environment = new Dictionary<string, string>
            {
                ["IMMICH_GO_UPLOAD_SERVER"] = server.Url.Value,
                ["IMMICH_GO_UPLOAD_API_KEY"] = key
            },
            Network = Network
        };

        log.Status($"Importing {takeout.Parts.Count} part{(takeout.Parts.Count == 1 ? "" : "s")} into {server.Url.Value}.");
        await docker.Run(run, ct);
        return StepResult.Successful;
    }
}
