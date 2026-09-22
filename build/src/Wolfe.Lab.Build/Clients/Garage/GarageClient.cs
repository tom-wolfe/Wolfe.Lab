using System.Text.RegularExpressions;

namespace Wolfe.Lab.Build.Clients.Garage;

/// <summary>
/// <c>docker exec garage /garage …</c>.
/// </summary>
internal sealed partial class GarageClient(ICommandRunner commands) : IGarage
{
    internal const string Container = "garage";

    /// <inheritdoc />
    public async Task<bool> IsReady(CancellationToken ct = default) =>
        (await commands.Run(Garage("status").QuietOutput(), ct)).IsSuccess;

    /// <inheritdoc />
    public async Task<int> LayoutVersion(CancellationToken ct = default)
    {
        var result = await commands.Run(Garage("layout", "show").QuietOutput().ThrowOnError(), ct);
        var match = VersionLine().Match(result.StandardOutput);
        return match.Success
            ? int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)
            : throw new CommandFailedException("garage layout show did not say which layout version is current.", result);
    }

    /// <inheritdoc />
    public async Task<string> NodeId(CancellationToken ct = default)
    {
        var result = await commands.Run(Garage("node", "id", "-q").QuietOutput().ThrowOnError(), ct);
        // The layout commands take the id's prefix; sixteen characters is what the docs use.
        var id = result.StandardOutput.Trim();
        return id.Length > 16 ? id[..16] : id;
    }

    /// <inheritdoc />
    public async Task AssignLayout(string nodeId, string zone, string capacity, CancellationToken ct = default) =>
        await commands.Run(Garage("layout", "assign", "-z", zone, "-c", capacity, nodeId).ThrowOnError(), ct);

    /// <inheritdoc />
    public async Task ApplyLayout(int version, CancellationToken ct = default) =>
        await commands.Run(Garage("layout", "apply", "--version", version.ToString(System.Globalization.CultureInfo.InvariantCulture)).ThrowOnError(), ct);

    private static Command Garage(params string[] arguments) =>
        Command.Create("docker").WithArguments(["exec", Container, "/garage", .. arguments]);

    [GeneratedRegex(@"^Current cluster layout version: (\d+)", RegexOptions.Multiline)]
    private static partial Regex VersionLine();
}
