using Microsoft.Extensions.Configuration;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Nodes;

namespace Wolfe.Lab.Infrastructure.Releases;

/// <summary>
/// Exposes well known directories in the lab.
/// </summary>
public sealed class LabDirectories
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// The variable naming where components are installed.
    /// </summary>
    public const string RootVariable = "LAB_ROOT";
    internal const string DataVariable = "LAB_DATA";

    /// <summary>
    /// Where components and their artifacts are installed: <c>LAB_ROOT</c>.
    /// </summary>
    public IDirectory Root { get; set; } = new PhysicalDirectory(Path.Combine(Home, ".local", "share", "Wolfe.Lab"));

    /// <summary>
    /// Where services keep their state: <c>LAB_DATA</c>.
    /// </summary>
    public IDirectory Data { get; set; } = new PhysicalDirectory(Path.Combine(Home, "Docker"));

    /// <summary>
    /// The directories <paramref name="node"/> declares it keeps the lab in, wherever this runs.
    /// </summary>
    public static LabDirectories ForNode(Node node) => new()
    {
        Root = new PhysicalDirectory(node.Directories.Root.Value),
        Data = new PhysicalDirectory(node.Directories.Data.Value)
    };

    /// <summary>
    /// Takes each root <paramref name="configuration"/> sets over its default; one it leaves
    /// empty keeps it. The configuration reads the environment with its prefix stripped, so
    /// <c>LAB_ROOT</c> is <c>ROOT</c> there.
    /// </summary>
    internal void Configure(IConfiguration configuration)
    {
        if (configuration[RootVariable[LabConfiguration.EnvironmentPrefix.Length..]] is { Length: > 0 } root)
        {
            Root = new PhysicalDirectory(root);
        }

        if (configuration[DataVariable[LabConfiguration.EnvironmentPrefix.Length..]] is { Length: > 0 } data)
        {
            Data = new PhysicalDirectory(data);
        }
    }

    /// <summary>
    /// Gets where <paramref name="unit"/> is installed on the node.
    /// </summary>
    public IDirectory DeployedTo(DeploymentUnit unit) => Root.GetDirectory(unit.Name);

    /// <summary>
    /// Gets the stamp file for the given deployment unit.
    /// </summary>
    public IFile AppliedStamp(DeploymentUnit unit) => Applied.GetFile(unit.Name);

    /// <summary>
    /// Where every compose stack's restart stamp is kept.
    /// </summary>
    public IDirectory Applied => Root.GetDirectory(".applied");

    /// <summary>
    /// Where a component's agents say which log files they write, for the node's collector to
    /// find (monitoring/alloy): one target file per component, beside the releases.
    /// </summary>
    public IDirectory Logs => Root.GetDirectory(".logs");

    /// <summary>
    /// Where a deploy says which metrics endpoints its components serve, for the node's collector
    /// to scrape (monitoring/alloy): one target file per component, beside the releases.
    /// </summary>
    public IDirectory Metrics => Root.GetDirectory(".metrics");

    /// <summary>
    /// Where an agent the catalog declares writes its output, both streams: a file of its
    /// component's, beside the releases, on every node alike.
    /// </summary>
    public IFile AgentLog(string qualifiedName) => Root.GetDirectory("logs").GetFile($"{qualifiedName}.log");
}
