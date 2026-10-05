namespace Wolfe.Lab.Domain.Catalog.Nodes;

/// <summary>
/// A node's name, the lab's for the machine: <c>mini</c>, <c>studio</c>, <c>pi</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct NodeName
{
    private static Validation Validate(string input) => NameRule.Validate(input, "a node's name");
}
