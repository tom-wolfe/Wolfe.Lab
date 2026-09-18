using Wolfe.Lab.Build.Deploy.Models;

namespace Wolfe.Lab.Build.Files.Models;

/// <summary>
/// The shape of <c>files/ritten.json</c>: a slice that is data, so nothing beyond what every
/// slice declares.
/// </summary>
public sealed record FilesSettings : SliceSettings;
