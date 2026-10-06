using Ritten.Engine.FileSystem;

namespace Wolfe.Lab.Tests;

/// <summary>
/// A substitute file system whose scratch directories are real, in the system's temporary directory, as Ritten's are:
/// for steps that hand one to a tool, which writes into it.
/// </summary>
internal static class ScratchFileSystem
{
    public static IFileSystem Create(IFileSystem? fileSystem = null)
    {
        fileSystem ??= Substitute.For<IFileSystem>();
        fileSystem.CreateTempDirectory(Arg.Any<string>())
            .Returns(call => new PhysicalDirectory(Directory.CreateTempSubdirectory(call.Arg<string>()).FullName));
        return fileSystem;
    }
}
