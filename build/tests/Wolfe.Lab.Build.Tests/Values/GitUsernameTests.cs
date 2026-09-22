using Vogen;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Tests.Values;

public class GitUsernameTests
{
    [Fact]
    public void From_TrimsTheName()
    {
        GitUsername.From(" tom-wolfe\n").Value.ShouldBe("tom-wolfe");
    }

    [Theory]
    [InlineData("")]
    [InlineData("tom wolfe")]
    [InlineData("tom:secret")]
    public void From_RefusesWhatACredentialHelperWouldMisread(string text)
    {
        Should.Throw<ValueObjectValidationException>(() => GitUsername.From(text));
    }
}
