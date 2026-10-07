using Microsoft.Extensions.Configuration;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Tests.Infrastructure.Releases;

public class LabDirectoriesTests
{
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
}
