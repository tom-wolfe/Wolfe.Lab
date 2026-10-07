using Wolfe.Lab.Domain.Catalog.Components;
using System.Reflection;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;
using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// The schema of a value from one of the domain's closed sets (<see cref="IClosedSet{TSelf}"/>).
/// </summary>
internal sealed class ClosedSetSchemas : ISchemaGenerator
{
    /// <summary>
    /// Empty instance.
    /// </summary>
    public static ClosedSetSchemas Instance { get; } = new();

    /// <inheritdoc />
    public bool Handles(Type type) => type
        .GetInterfaces()
        .Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IClosedSet<>));

    /// <inheritdoc />
    public void AddConstraints(SchemaGenerationContextBase context)
    {
        // A static abstract member, so the set's own property, read once per type the schema names.
        var all = context.Type
                .GetProperty(nameof(IClosedSet<>.All), BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) as System.Collections.IEnumerable
                  ?? throw new InvalidOperationException($"{context.Type.Name} is a closed set with no values.");
        context.Intents.Add(new TypeIntent(SchemaValueType.String));
        var values = all.Cast<object>().Select(value => value.ToString() ?? "");

        // What a workflow was once called, read as what it is now until no declaration writes it.
        if (context.Type == typeof(WorkflowName))
        {
            values = values.Concat(WorkflowName.Formerly.Keys);
        }

        context.Intents.Add(new EnumIntent(values));
    }
}
