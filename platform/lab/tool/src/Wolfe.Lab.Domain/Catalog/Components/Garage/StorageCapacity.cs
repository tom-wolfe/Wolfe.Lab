namespace Wolfe.Lab.Domain.Catalog.Components.Garage;

/// <summary>
/// How much a Garage node stores, in bytes.
/// </summary>
[ValueObject<long>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct StorageCapacity
{
    private static Validation Validate(long input) =>
        input > 0 ? Validation.Ok : Validation.Invalid($"{input} is no capacity: a size in bytes, above zero.");
}
