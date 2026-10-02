using Ritten.Engine.FileSystem;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Infrastructure.Paths;

/// <summary>
/// A <see cref="HostPath"/> as the file system reads it: the domain holds where a directory is, and
/// only the infrastructure opens it.
/// </summary>
public static class HostPaths
{
    extension(HostPath path)
    {
        /// <summary>
        /// The path as a directory on this node.
        /// </summary>
        public IDirectory Directory => new PhysicalDirectory(path.Value);
    }
}
