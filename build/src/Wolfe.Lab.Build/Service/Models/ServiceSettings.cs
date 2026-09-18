using Wolfe.Lab.Build.Deploy.Models;

namespace Wolfe.Lab.Build.Service.Models;

/// <summary>
/// The shape of a compose slice's <c>ritten.json</c> when the CLI's part in it is the backup:
/// nothing beyond what every slice declares.
/// </summary>
public sealed record ServiceSettings : SliceSettings;
