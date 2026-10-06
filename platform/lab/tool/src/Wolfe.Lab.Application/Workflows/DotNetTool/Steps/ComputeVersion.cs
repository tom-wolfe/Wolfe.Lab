using System.Globalization;
using System.Xml.Linq;
using NuGet.Versioning;
using Ritten.Git;
using Wolfe.Lab.Application.Workflows.DotNetTool.Models;

namespace Wolfe.Lab.Application.Workflows.DotNetTool.Steps;

/// <summary>
/// Works out the version this merge publishes.
/// </summary>
[Step("compute version", StepKind.Work)]
internal sealed class ComputeVersion(IGit git, IFileSystem fileSystem, PackageContents shipped, IOptions<GitOptions> options, IWorkflowLog log)
{
    /// <summary>
    /// Where the version is written, under the component; <c>Directory.Build.props</c> names it too.
    /// </summary>
    internal const string VersionFile = "temp/version.props";

    /// <summary>
    /// The property the props file sets, which <c>Directory.Build.props</c> reads.
    /// </summary>
    internal const string VersionProperty = "LabVersion";

    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        var component = fileSystem.ProjectRoot;
        if (await git.RepositoryRoot(ct) is not { } checkout)
        {
            return new Error($"{component.AbsolutePath} is not in a git checkout, and its releases are its tags.");
        }

        var repository = git.InRepository(checkout);
        if (await repository.IsShallow(ct))
        {
            return new Error("The checkout is shallow, so it may not hold the last release: check out the whole history (fetch-depth: 0).");
        }

        var prefix = options.Value.TagPrefix;
        var last = LastRelease(await repository.Tags($"{prefix}*", ct), prefix);

        string version;
        if (last is null)
        {
            version = Next(null);
            log.Status($"Version {version}: the first release.");
        }
        else
        {
            var changed = await Changed(repository, $"{prefix}{last}", shipped.FromRoot(checkout, component), ct);

            version = changed ? Next(last) : last.ToNormalizedString();
            log.Status(changed
                ? $"Version {version}: what ships has changed since {prefix}{last}."
                : $"Version {version}: nothing that ships has changed since it was released.");
        }

        var props = new XDocument(new XElement("Project", new XElement("PropertyGroup", new XElement(VersionProperty, version))));
        await component.GetFile(VersionFile).WriteAllText(props.ToString(), cancellationToken: ct);
        return StepResult.Successful;
    }

    /// <summary>
    /// The highest version among the release tags, or null when there are none.
    /// </summary>
    private static NuGetVersion? LastRelease(IReadOnlyList<string> tags, string prefix) =>
        tags.Where(tag => tag.StartsWith(prefix, StringComparison.Ordinal))
            .Select(tag => NuGetVersion.TryParse(tag[prefix.Length..], out var version) ? version : null)
            .OfType<NuGetVersion>()
            .Max();

    /// <summary>
    /// The release after <paramref name="last"/>: <c>1.0.&lt;n+1&gt;</c>.
    /// </summary>
    private static string Next(NuGetVersion? last) =>
        string.Create(CultureInfo.InvariantCulture, $"1.0.{(last?.Patch ?? 0) + 1}");

    /// <summary>
    /// Whether anything that ships has changed since <paramref name="release"/>: the tag is an ancestor of HEAD, so
    /// against its merge base is against the tag itself.
    /// </summary>
    private static async Task<bool> Changed(IGit repository, string release, IReadOnlyList<string> paths, CancellationToken ct)
    {
        foreach (var path in paths)
        {
            if ((await repository.ChangedFilesSince(release, path, ct)).Count > 0)
            {
                return true;
            }
        }

        return false;
    }
}
