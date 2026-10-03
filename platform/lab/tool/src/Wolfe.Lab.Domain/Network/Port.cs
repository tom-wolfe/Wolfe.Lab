namespace Wolfe.Lab.Domain.Network;

/// <summary>
/// A TCP port: <c>8081</c>.
/// </summary>
[ValueObject<int>(conversions: Conversions.TypeConverter)]
public readonly partial struct Port
{
    private static Validation Validate(int input) =>
        input is >= 1 and <= 65535 ? Validation.Ok : Validation.Invalid(NetworkErrors.NotAPort(input).Message);
}
