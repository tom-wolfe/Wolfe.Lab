using Vogen;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Tests.Domain.Paths;

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

    [Theory]
    [InlineData("${LAB_ROOT}/alloy/config.alloy")]
    [InlineData("${LAB_DATA}/alloy")]
    [InlineData("${PACKAGE}/alloy-darwin-arm64")]
    public void From_TakesAPathFromOneOfTheLabsRoots(string path) =>
        HostPath.From(path).Value.ShouldBe(path);

    [Theory]
    [InlineData("${HOME}/elsewhere")]
    [InlineData("relative/path")]
    public void From_StillRefusesAPathThatIsNotAbsolute(string path) =>
        Should.Throw<ValueObjectValidationException>(() => HostPath.From(path));
}
