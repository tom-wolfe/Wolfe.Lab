using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Detection;

/// <summary>
/// Reads schema.org JSON-LD out of a message's HTML — the tier that needs no model at all.
/// </summary>
/// <remarks>
/// Bookings, tickets and reservations carry this, and it is how Gmail does most of what it
/// does: deterministic, high precision, no inference. A reservation nests the thing being
/// reserved under <c>reservationFor</c>, so the walk recurses rather than reading the top
/// level — the same pass finds a bare Event and a flight.
/// </remarks>
internal static partial class StructuredEvents
{
    [GeneratedRegex(
        """<script[^>]*type\s*=\s*["']application/ld\+json["'][^>]*>(.*?)</script>""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Block();

    /// <summary>
    /// Every event the HTML declares, best first — an object naming itself an Event is better
    /// evidence than one merely carrying a start date.
    /// </summary>
    /// <param name="html">The message's HTML body.</param>
    public static IReadOnlyList<DetectedEvent> Read(string? html)
    {
        if (html is null)
        {
            return [];
        }

        var found = new List<(int Rank, DetectedEvent Event)>();
        foreach (Match block in Block().Matches(html))
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(block.Groups[1].Value);
            }
            catch (JsonException)
            {
                // A page with one malformed block still has its other blocks.
                continue;
            }

            using (document)
            {
                Walk(document.RootElement, found);
            }
        }

        return [.. found.OrderBy(candidate => candidate.Rank).Select(candidate => candidate.Event)];
    }

    private static void Walk(JsonElement element, List<(int, DetectedEvent)> found)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Walk(item, found);
                }

                return;

            case JsonValueKind.Object:
                if (ToEvent(element) is { } detected)
                {
                    found.Add(detected);
                }

                foreach (var property in element.EnumerateObject())
                {
                    Walk(property.Value, found);
                }

                return;
        }
    }

    private static (int Rank, DetectedEvent Event)? ToEvent(JsonElement element)
    {
        if (Text(element, "startDate") is not { } start || !TryDate(start, out var begins))
        {
            return null;
        }

        if (Text(element, "name") is not { Length: > 0 } name)
        {
            return null;
        }

        var end = Text(element, "endDate") is { } finish && TryDate(finish, out var ends) ? ends : (DateTimeOffset?)null;

        // An explicit Event beats an object that merely has a date on it — a reservation's
        // outer wrapper sometimes carries one too.
        var rank = Type(element) is { } type && type.EndsWith("Event", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

        return (rank, new DetectedEvent(name, begins, end, Place(element), EventSource.StructuredData));
    }

    private static string? Type(JsonElement element) =>
        element.TryGetProperty("@type", out var type) && type.ValueKind == JsonValueKind.String
            ? type.GetString()
            : null;

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// A location is a string, or an object with a name, or an object with a postal address —
    /// schema.org allows all three and senders use all three.
    /// </summary>
    private static string? Place(JsonElement element)
    {
        if (!element.TryGetProperty("location", out var location))
        {
            return null;
        }

        if (location.ValueKind == JsonValueKind.String)
        {
            return location.GetString();
        }

        if (location.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var name = Text(location, "name");
        var street = location.TryGetProperty("address", out var address)
            ? address.ValueKind == JsonValueKind.String ? address.GetString() : Text(address, "streetAddress")
            : null;

        return string.Join(", ", new[] { name, street }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    /// <summary>
    /// schema.org dates are ISO 8601, with or without a time and with or without an offset. One
    /// without an offset is the sender's local time and is taken at face value rather than
    /// guessed at.
    /// </summary>
    private static bool TryDate(string value, out DateTimeOffset when) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out when);
}
