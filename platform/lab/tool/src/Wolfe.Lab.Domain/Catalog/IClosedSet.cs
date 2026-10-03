namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Identifies a value object whose values are a fixed list like an enum.
/// </summary>
/// <typeparam name="TSelf">The value object.</typeparam>
public interface IClosedSet<out TSelf> where TSelf : IClosedSet<TSelf>
{
    /// <summary>
    /// Lists every option in the set.
    /// </summary>
    static abstract IReadOnlyList<TSelf> All { get; }
}
