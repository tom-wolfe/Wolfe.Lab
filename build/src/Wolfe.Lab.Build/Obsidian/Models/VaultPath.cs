using Ritten.Engine.FileSystem;
using Vogen;

namespace Wolfe.Lab.Build.Obsidian.Models;

/// <summary>
/// Where a vault's checkout is on the node.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct VaultPath
{
    /// <summary>
    /// The checkout as a directory.
    /// </summary>
    public IDirectory Directory => new PhysicalDirectory(Value);

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

    private static Validation Validate(string input) =>
        input.Length > 0 && Path.IsPathRooted(input)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' must be an absolute path, or start with ~/.");
}
