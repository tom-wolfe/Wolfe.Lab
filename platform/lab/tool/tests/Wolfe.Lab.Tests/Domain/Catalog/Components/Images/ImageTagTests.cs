using Wolfe.Lab.Domain.Catalog.Components.Images;

namespace Wolfe.Lab.Tests.Domain.Catalog.Components.Images;

public class ImageTagTests
{
    [Theory]
    [InlineData("code.twolfe.dev/tom-wolfe/ci", "code.twolfe.dev")]
    [InlineData("code.twolfe.dev/tom-wolfe/ci:1.2", "code.twolfe.dev")]
    [InlineData("localhost:5000/ci", "localhost:5000")]
    public void From_TakesATagThatNamesItsRegistry(string tag, string registry) =>
        ImageTag.From(tag).Registry.ShouldBe(registry);

    // Either would be pushed to Docker Hub.
    [Theory]
    [InlineData("lab/ci")]
    [InlineData("ci")]
    public void TryFrom_RefusesATagThatNamesNoRegistry(string tag) =>
        ImageTag.TryFrom(tag).IsSuccess.ShouldBeFalse();
}
