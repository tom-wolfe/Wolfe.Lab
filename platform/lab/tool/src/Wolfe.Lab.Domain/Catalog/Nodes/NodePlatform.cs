namespace Wolfe.Lab.Domain.Catalog.Nodes;

/// <summary>
/// The operating system and architecture a node runs, as release assets name them:
/// <c>darwin-arm64</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
[Instance("DarwinArm64", "darwin-arm64", "macOS on Apple silicon: the Macs.")]
[Instance("LinuxArm64", "linux-arm64", "Linux on 64-bit ARM: the Pi.")]
[Instance("LinuxAmd64", "linux-amd64", "Linux on x86-64: the coming primary node.")]
public readonly partial struct NodePlatform : IClosedSet<NodePlatform>
{
    /// <inheritdoc />
    public static IReadOnlyList<NodePlatform> All => [DarwinArm64, LinuxArm64, LinuxAmd64];

    /// <summary>
    /// The operating system alone: <c>darwin</c>, <c>linux</c>.
    /// </summary>
    public string Os => Value[..Value.IndexOf('-', StringComparison.Ordinal)];

    /// <summary>
    /// The architecture alone: <c>arm64</c>, <c>amd64</c>.
    /// </summary>
    public string Arch => Value[(Value.IndexOf('-', StringComparison.Ordinal) + 1)..];

    private static Validation Validate(string input) =>
        All.Any(platform => platform.Value == input)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not a platform ({string.Join(", ", All)}).");
}
