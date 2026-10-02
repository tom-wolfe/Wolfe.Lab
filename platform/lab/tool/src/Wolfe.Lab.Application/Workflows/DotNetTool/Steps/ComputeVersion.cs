using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Git;

namespace Wolfe.Lab.Application.Workflows.DotNetTool.Steps;

/// <summary>
/// Works out the version this merge publishes.
/// </summary>
[Step("compute version", StepKind.Work)]
internal sealed class ComputeVersion(ICommandRunner commands, IGit git, IFileSystem fileSystem, ShippedInputs shipped, IOptions<GitOptions> options, IWorkflowLog log)
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
        var component = fileSystem.ProjectRoot.AbsolutePath;
        if (await git.RepositoryRoot(ct) is not { } checkout)
        {
            return new Error($"{component} is not in a git checkout, and its releases are its tags.");
        }

        var root = checkout.AbsolutePath;
        var shallow = await commands.Run(Git(root, "rev-parse", "--is-shallow-repository"), ct);
        if (shallow.StandardOutput.Trim() == "true")
        {
            return new Error("The checkout is shallow, so it may not hold the last release: check out the whole history (fetch-depth: 0).");
        }

        var prefix = options.Value.TagPrefix;
        var tags = await commands.Run(Git(root, "tag", "--list", $"{prefix}*"), ct);
        var last = LastRelease(tags.StandardOutput, prefix);

        string version;
        if (last is null)
        {
            version = Next(null);
            log.Status($"Version {version}: the first release.");
        }
        else
        {
            var diff = await commands.Run(
                Command.Create("git").WithArguments(["diff", "--quiet", $"{prefix}{last}", "HEAD", "--", .. shipped.FromRoot(root, component)])
                    .InDirectory(root).QuietOutput(),
                ct);
            var changed = diff.ExitCode.Value switch
            {
                0 => false,
                1 => true,
                _ => throw new InvalidOperationException($"git diff against {prefix}{last} failed: {diff.StandardError.Trim()}")
            };

            version = changed ? Next(last) : last.ToNormalizedString();
            log.Status(changed
                ? $"Version {version}: what ships has changed since {prefix}{last}."
                : $"Version {version}: nothing that ships has changed since it was released.");
        }

        var file = Path.Combine(component, VersionFile);
        Directory.CreateDirectory(Path.GetDirectoryName(file) ?? component);
        new XDocument(new XElement("Project", new XElement("PropertyGroup", new XElement(VersionProperty, version)))).Save(file);
        return StepResult.Successful;
    }

    /// <summary>
    /// The highest version among the release tags, or null when there are none.
    /// </summary>
    internal static NuGetVersion? LastRelease(string tags, string prefix) =>
        tags.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => tag.StartsWith(prefix, StringComparison.Ordinal))
            .Select(tag => NuGetVersion.TryParse(tag[prefix.Length..], out var version) ? version : null)
            .OfType<NuGetVersion>()
            .Max();

    /// <summary>
    /// The release after <paramref name="last"/>: <c>1.0.&lt;n+1&gt;</c>.
    /// </summary>
    internal static string Next(NuGetVersion? last) =>
        string.Create(CultureInfo.InvariantCulture, $"1.0.{(last?.Patch ?? 0) + 1}");

    private static Command Git(string directory, params string[] arguments) =>
        Command.Create("git").WithArguments(arguments).InDirectory(directory).QuietOutput().ThrowOnError();
}
