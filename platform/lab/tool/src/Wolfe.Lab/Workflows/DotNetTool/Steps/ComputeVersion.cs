using System.Xml.Linq;

namespace Wolfe.Lab.Workflows.DotNetTool.Steps;

/// <summary>
/// Works out the version this merge publishes: <c>1.0.&lt;n&gt;</c>, where <c>n</c> counts the
/// commits that changed what ships.
/// </summary>
/// <remarks>
/// A merge that changes nothing that ships keeps the number, so the feed already has it and the
/// release gate stops. The version reaches MSBuild as a props file in the component's scratch,
/// which <c>Directory.Build.props</c> imports, so every <c>dotnet</c> the job runs — reading the
/// projects, packing — sees it without the process's environment being touched.
/// </remarks>
[Step("compute version", StepKind.Work)]
internal sealed class ComputeVersion(ICommandRunner commands, IFileSystem fileSystem, ShippedInputs shipped, IWorkflowLog log)
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
        var shallow = await commands.Run(Git(component, "rev-parse", "--is-shallow-repository"), ct);
        if (shallow.StandardOutput.Trim() == "true")
        {
            return new Error("The checkout is shallow, so its commits cannot be counted: check out the whole history (fetch-depth: 0).");
        }

        var counted = await commands.Run(Git(component, ["rev-list", "--count", "HEAD", "--", .. shipped.Paths]), ct);
        var version = Version(int.Parse(counted.StandardOutput.Trim(), System.Globalization.CultureInfo.InvariantCulture));

        var file = Path.Combine(component, VersionFile);
        Directory.CreateDirectory(Path.GetDirectoryName(file) ?? component);
        new XDocument(new XElement("Project", new XElement("PropertyGroup", new XElement(VersionProperty, version)))).Save(file);

        log.Status($"Version {version}: {Path.GetFileName(component)} has changed what it ships {counted.StandardOutput.Trim()} times.");
        return StepResult.Successful;
    }

    /// <summary>
    /// The version for <paramref name="changes"/> commits that changed what ships.
    /// </summary>
    private static string Version(int changes) => $"1.0.{changes}";

    private static Command Git(string directory, params string[] arguments) =>
        Command.Create("git").WithArguments(arguments).InDirectory(directory).QuietOutput().ThrowOnError();
}
