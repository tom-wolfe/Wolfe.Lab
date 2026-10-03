using Vogen;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Describes the general category of the component.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
[Instance("Workload", "workload", "Something that runs and keeps running: a compose stack, a host agent, an API.")]
[Instance("Backup", "backup", "Something that is only a backup: an Obsidian vault.")]
[Instance("Runner", "runner", "A CI runner.")]
[Instance("Repository", "repository", "A backup repository.")]
[Instance("Infrastructure", "infrastructure", "Resources declared to an API: a tofu root.")]
[Instance("Certificate", "certificate", "A certificate, kept renewed.")]
[Instance("Package", "package", "Something built and published: an image, a tool.")]
[Instance("Machine", "machine", "The nodes' profiles: the kernel's.")]
public readonly partial struct ComponentKind : IClosedSet<ComponentKind>
{
    /// <inheritdoc />
    public static IReadOnlyList<ComponentKind> All => [Workload, Backup, Runner, Repository, Infrastructure, Certificate, Package, Machine];

    private static Validation Validate(string input) =>
        All.Any(kind => kind.Value == input)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not a kind of component ({string.Join(", ", All)}).");
}
