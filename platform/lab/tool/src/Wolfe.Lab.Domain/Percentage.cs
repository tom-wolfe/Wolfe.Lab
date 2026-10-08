namespace Wolfe.Lab.Domain;

/// <summary>
/// A share of a whole, in whole percent.
/// </summary>
[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct Percentage
{
    private static Validation Validate(int input) =>
        input is > 0 and <= 100 ? Validation.Ok : Validation.Invalid($"{input}% is no share: above 0%, and at most 100%.");

    /// <inheritdoc />
    public override string ToString() => $"{Value}%";
}
