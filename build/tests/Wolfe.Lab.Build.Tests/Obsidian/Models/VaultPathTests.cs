using Vogen;
using Wolfe.Lab.Build.Obsidian.Models;

namespace Wolfe.Lab.Build.Tests.Obsidian.Models;

public class VaultPathTests
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [Fact]
    public void From_ExpandsTheHomeShorthand()
    {
        VaultPath.From("~/Obsidian/main").Value.ShouldBe(Path.Combine(Home, "Obsidian", "main"));
        VaultPath.From("~").Value.ShouldBe(Home);
    }

    [Fact]
    public void From_KeepsAnAbsolutePathAsWritten()
    {
        VaultPath.From(" /Volumes/Data2/vault ").Value.ShouldBe("/Volumes/Data2/vault");
    }

    [Fact]
    public void Directory_IsTheCheckout()
    {
        VaultPath.From("~/Obsidian/main").Directory.AbsolutePath.ShouldBe(Path.Combine(Home, "Obsidian", "main"));
    }

    [Theory]
    [InlineData("Obsidian/main")]
    [InlineData("~Obsidian")]
    [InlineData("")]
    public void From_RefusesARelativePath(string text)
    {
        Should.Throw<ValueObjectValidationException>(() => VaultPath.From(text));
    }
}
