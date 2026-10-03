namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// One service of a <see cref="ComposeProject"/>.
/// </summary>
/// <param name="Name">The service's name in the compose file.</param>
/// <param name="Labels">Its Docker labels.</param>
/// <param name="Environment">Its environment; a variable declared without a value is null.</param>
public sealed record ComposeService(string Name, IReadOnlyDictionary<string, string> Labels, IReadOnlyDictionary<string, string?> Environment);
