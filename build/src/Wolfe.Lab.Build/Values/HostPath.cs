using Ritten.Engine.FileSystem;
using Vogen;

namespace Wolfe.Lab.Build.Values;

/// <summary>
/// A directory on the node. Written the way a person types it, <c>~</c> included; held the way
/// the machine reads it, so everything downstream sees one absolute path.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct HostPath
{
    /// <summary>
    /// The path as a directory.
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
