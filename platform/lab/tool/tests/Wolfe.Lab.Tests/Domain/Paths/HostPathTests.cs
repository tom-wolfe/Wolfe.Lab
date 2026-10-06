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

    [Fact]
    public void From_TakesAPathInAnAgentsPackage() =>
        HostPath.From("{package}/alloy-darwin-arm64").Value.ShouldBe("{package}/alloy-darwin-arm64");

    [Theory]
    [InlineData("${HOME}/elsewhere")]
    [InlineData("${LAB_ROOT}/alloy/config.alloy")]
    [InlineData("${PACKAGE}/alloy-darwin-arm64")]
    [InlineData("relative/path")]
    public void From_StillRefusesAPathThatIsNotAbsolute(string path) =>
        Should.Throw<ValueObjectValidationException>(() => HostPath.From(path));
}
