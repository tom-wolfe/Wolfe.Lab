using System.Globalization;
using Wolfe.Lab.Domain;

namespace Wolfe.Lab.Tests.Domain;

public class PercentageTests
{
    [Fact]
    public void ToString_IncludesPercentageSymbol()
    {
        // Arrange
        var value = Percentage.From(10);

        // Act
        var result = value.ToString(CultureInfo.InvariantCulture);

        // Assert
        result.ShouldBe("10%");
    }

    // [Fact]
    // public void StringFormat_IncludesPercentageSymbol()
    // {
    //     // Arrange
    //     var value = Percentage.From(10);
    //
    //     // Act
    //     var result = $"{value}";
    //
    //     // Assert
    //     result.ShouldBe("10%");
    // }
}
