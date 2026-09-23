namespace Wolfe.Lab.Build.Clients.Releases;

/// <summary>
/// A component installed on the node: the copy it runs from after the job's workspace is gone.
/// </summary>
/// <remarks>
/// A runner checks the repository out into a disposable workspace, and a container bind-mounts
/// files — config directories, a Caddyfile, route snippets — that it goes on reading long after
/// the job that started it has been cleaned away. So a component is installed under a name of
/// its own into one flat root, and everything the node runs, runs from there.
/// </remarks>
/// <param name="Name">The name the component is installed under.</param>
/// <param name="Directory">The installed copy.</param>
public sealed record Release(string Name, IDirectory Directory);
