using Microsoft.Extensions.Configuration;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Tests.Infrastructure.Releases;

public class LabDirectoriesTests
{
    private static readonly LabDirectories Directories = new() { Root = new PhysicalDirectory("/lab/root"), Data = new PhysicalDirectory("/lab/data") };

    // As the lab's configuration has them: the environment's LAB_ prefix stripped.
    private static LabDirectories Configured(params (string Key, string Value)[] settings)
    {
        var roots = new LabDirectories();
        roots.Configure(new ConfigurationBuilder().AddInMemoryCollection(settings.Select(setting => KeyValuePair.Create(setting.Key, (string?)setting.Value))).Build());
        return roots;
    }

    [Fact]
    public void Configure_TakesBothRootsFromTheConfiguration()
    {
        var roots = Configured(("ROOT", "/r"), ("DATA", "/d"));

        roots.Root.AbsolutePath.ShouldBe("/r");
        roots.Data.AbsolutePath.ShouldBe("/d");
    }

    [Fact]
    public void Configure_KeepsWhatTheyHaveAlwaysBeen_ForOneUnsetOrEmpty()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var roots = Configured(("ROOT", ""));

        roots.Root.AbsolutePath.ShouldBe(Path.Combine(home, ".local", "share", "Wolfe.Lab"));
        roots.Data.AbsolutePath.ShouldBe(Path.Combine(home, "Docker"));
    }

    [Fact]
    public void Expand_ReplacesTheLabsRootsAndNothingElse() =>
        Directories.Expand("${LAB_ROOT}/alloy:${LAB_DATA}/x:${HOME}").ShouldBe("/lab/root/alloy:/lab/data/x:${HOME}");

    [Theory]
    [InlineData("/lab/root/alloy", true)]
    [InlineData("/lab/root/a/b", true)]
    [InlineData("/lab/root", false)]
    [InlineData("/lab/rootless", false)]
    [InlineData("/lab/root/../elsewhere", false)]
    [InlineData("/Users/tomwolfe", false)]
    public void Contains_IsStrictlyInsideTheInstallRoot(string path, bool expected) =>
        Directories.Contains(new PhysicalDirectory(path)).ShouldBe(expected);
}
