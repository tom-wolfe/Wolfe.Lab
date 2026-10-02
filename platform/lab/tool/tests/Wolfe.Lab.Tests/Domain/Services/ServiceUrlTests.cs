using Vogen;
using Wolfe.Lab.Domain.Services;

namespace Wolfe.Lab.Tests.Domain.Services;

public class ServiceUrlTests
{
    [Fact]
    public void From_TrimsAndDropsATrailingSlash()
    {
        ServiceUrl.From(" http://immich-server:2283/ ").Value.ShouldBe("http://immich-server:2283");
    }

    [Theory]
    [InlineData("immich-server:2283")]
    [InlineData("ssh://host/repo")]
    [InlineData("")]
    public void From_RefusesWhatIsNotAnHttpUrl(string text)
    {
        Should.Throw<ValueObjectValidationException>(() => ServiceUrl.From(text));
    }
}
