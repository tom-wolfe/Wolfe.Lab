using Vogen;
using Wolfe.Lab.Build.Paths;

namespace Wolfe.Lab.Build.Tests.Paths;

public class HostPathTests
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [Fact]
    public void From_ExpandsTheHomeShorthand()
    {
        HostPath.From("~/Obsidian/main").Value.ShouldBe(Path.Combine(Home, "Obsidian", "main"));
        HostPath.From("~").Value.ShouldBe(Home);
    }

    [Fact]
    public void From_KeepsAnAbsolutePathAsWritten()
    {
        HostPath.From(" /Volumes/Data2/vault ").Value.ShouldBe("/Volumes/Data2/vault");
    }

    [Fact]
    public void Directory_IsTheCheckout()
    {
        HostPath.From("~/Obsidian/main").Directory.AbsolutePath.ShouldBe(Path.Combine(Home, "Obsidian", "main"));
    }

    [Theory]
    [InlineData("Obsidian/main")]
    [InlineData("~Obsidian")]
    [InlineData("")]
    public void From_RefusesARelativePath(string text)
    {
        Should.Throw<ValueObjectValidationException>(() => HostPath.From(text));
    }
}
