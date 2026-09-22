namespace Wolfe.Lab.Build.Workflows.Garage.Models;

/// <summary>
/// The node's role, as the job applies it.
/// </summary>
/// <param name="Zone">The zone.</param>
/// <param name="Capacity">The capacity, as Garage spells it.</param>
public sealed record Layout(string Zone, string Capacity);
