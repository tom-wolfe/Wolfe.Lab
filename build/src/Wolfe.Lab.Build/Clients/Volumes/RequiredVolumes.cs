namespace Wolfe.Lab.Build.Clients.Volumes;

/// <summary>
/// The external volumes a component binds: a job refuses to run while one is unmounted.
/// </summary>
/// <param name="Directories">The mount points.</param>
public sealed record RequiredVolumes(IReadOnlyList<IDirectory> Directories);
