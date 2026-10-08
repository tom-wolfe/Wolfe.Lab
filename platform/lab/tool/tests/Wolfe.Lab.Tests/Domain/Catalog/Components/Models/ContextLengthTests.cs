using Wolfe.Lab.Domain.Catalog.Components.Models;

namespace Wolfe.Lab.Tests.Domain.Catalog.Components.Models;

public class ContextLengthTests
{
    [Fact]
    public void From_TakesALengthInTokens() =>
        ContextLength.From(16384).Value.ShouldBe(16384);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryFrom_RefusesWhatIsNoLength(int tokens) =>
        ContextLength.TryFrom(tokens).IsSuccess.ShouldBeFalse();
}
