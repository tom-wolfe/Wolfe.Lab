using System.Text.RegularExpressions;

namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// What a component's roles must satisfy, alone and against the other components of the slice.
/// </summary>
internal static partial class RoleRules
{
    /// <summary>
    /// The problems with one component's roles, judged on its own file.
    /// </summary>
    /// <param name="models">The component's <c>models</c> section.</param>
    public static IEnumerable<string> Local(ModelSettings models)
    {
        foreach (var (name, role) in models.Roles.OrderBy(r => r.Key, StringComparer.Ordinal))
        {
            if (!RoleName().IsMatch(name))
            {
                yield return $"'{name}' cannot name a role: lowercase letters, digits and hyphens only.";
            }

            if (role.Model is not { } model)
            {
                yield return $"Role '{name}' names no 'model'.";
            }
            else if (!models.Pull.Contains(model))
            {
                yield return $"Role '{name}' points at {model.Value}, which 'models.pull' does not declare.";
            }
        }
    }

    /// <summary>
    /// The problems between components. Every role must be declared by every server, because
    /// the front door hands a request to whichever is up: a role only the Studio declares is
    /// "not found" every evening, where the point of a role is to degrade to the mini's best
    /// instead. A role marked identical must also name the same model everywhere.
    /// </summary>
    /// <param name="components">Every component of the slice, by name.</param>
    public static IEnumerable<string> Across(IReadOnlyDictionary<string, ModelSettings> components)
    {
        var names = components.Values
            .SelectMany(models => models.Roles.Keys)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal);

        foreach (var name in names)
        {
            var declared = components
                .OrderBy(c => c.Key, StringComparer.Ordinal)
                .Select(c => (Component: c.Key, Role: c.Value.Roles.GetValueOrDefault(name)))
                .ToList();

            var missing = declared.Where(d => d.Role is null).Select(d => d.Component).ToList();
            if (missing.Count > 0)
            {
                yield return $"Role '{name}' must be declared by every server, so it degrades rather than disappears when one is off; {string.Join(", ", missing)} does not declare it.";
            }

            var present = declared.Where(d => d.Role is not null).ToList();
            if (present.Any(d => d.Role!.Identical)
                && present.Select(d => (d.Role!.Model, d.Role.Identical)).Distinct().Count() > 1)
            {
                var each = present.Select(d => $"{d.Component}: {d.Role!.Model?.Value ?? "nothing"}{(d.Role.Identical ? "" : " (not marked identical)")}");
                yield return $"Role '{name}' must be identical everywhere, and is not: {string.Join("; ", each)}.";
            }
        }
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex RoleName();
}
