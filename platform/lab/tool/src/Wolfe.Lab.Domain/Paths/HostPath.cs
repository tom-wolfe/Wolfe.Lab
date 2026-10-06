using Ritten.Contracts.FileSystem;
using Ritten.Engine.FileSystem;

namespace Wolfe.Lab.Domain.Paths;

/// <summary>
/// A directory on the node. Written the way a person types it, <c>~</c> included; held the way
/// the machine reads it, so everything downstream sees one absolute path.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct HostPath
{
    /// <summary>
    /// The path as a directory on this node.
    /// </summary>
    public IDirectory Directory => new PhysicalDirectory(Value);

    /// <summary>
    /// The path as a file on this node.
    /// </summary>
    public IFile File => new PhysicalFile(Value);

    private static string NormalizeInput(string input)
    {
        var path = input.Trim();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return path switch
        {
            "~" => home,
            _ when path.StartsWith("~/", StringComparison.Ordinal) => Path.Combine(home, path[2..]),
            _ => path
        };
    }

    /// <summary>
    /// The roots a path may start from instead of <c>/</c>, expanded to one before anything uses
    /// it (platform/lab/README.md, "Two roots"): where the lab installs, where state lives, and an
    /// agent's own package.
    /// </summary>
    private static readonly string[] Variables = ["{package}"];

    private static Validation Validate(string input) =>
        input.Length > 0 && (Path.IsPathRooted(input) || Variables.Any(variable => input.StartsWith(variable, StringComparison.Ordinal)))
            ? Validation.Ok
            : Validation.Invalid($"'{input}' must be an absolute path, or start with one of: ~/, {string.Join(", ", Variables)}.");
}
