using Vogen;
using Wolfe.Lab.Build.Git;

namespace Wolfe.Lab.Build.Tests.Git;

public class RepositoryUrlTests
{
    [Theory]
    [InlineData("http://macmini.local:3000/tom-wolfe/obsidian-main.git")]
    [InlineData("https://git.twolfe.dev/tom-wolfe/obsidian-main.git")]
    [InlineData("ssh://git@git.twolfe.dev/tom-wolfe/obsidian-main.git")]
    public void From_KeepsAnAbsoluteGitUrl(string url)
    {
        RepositoryUrl.From($" {url} ").Value.ShouldBe(url);
    }

    [Theory]
    [InlineData("tom-wolfe/obsidian-main.git")]
    [InlineData("git@git.twolfe.dev:tom-wolfe/obsidian-main.git")]
    [InlineData("file:///tmp/repo")]
    [InlineData("")]
    public void From_RefusesWhatGitCouldNotPushTo(string text)
    {
        Should.Throw<ValueObjectValidationException>(() => RepositoryUrl.From(text));
    }
}
