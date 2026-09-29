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
/// <para>
/// A travel reservation keeps its times where its type does: a flight's and a train's on the
/// trip (<c>departureTime</c>, <c>arrivalTime</c>), a stay's on the reservation itself
/// (<c>checkinTime</c>, <c>checkoutTime</c>). Each type is read for what it is, with its booking
/// reference; an event reservation needs nothing of its own, since the Event it wraps is found.
/// </para>
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

        return [.. found.OrderBy(candidate => candidate.Rank).Select(candidate => candidate.Event)
            .DistinctBy(detected => (detected.Summary, detected.Start))];
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
                if ((ToReservation(element) ?? ToEvent(element)) is { } detected)
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

    private static (int Rank, DetectedEvent Event)? ToReservation(JsonElement reservation)
    {
        var reference = Text(reservation, "reservationNumber");
        var target = reservation.TryGetProperty("reservationFor", out var reserved) && reserved.ValueKind == JsonValueKind.Object
            ? reserved
            : default;

        DetectedEvent? detected = Type(reservation) switch
        {
            "FlightReservation" when target.ValueKind == JsonValueKind.Object =>
                Journey(target, "departureTime", "arrivalTime", Flight(target),
                    Name(target, "departureAirport"), Name(target, "arrivalAirport"), reference),
            "TrainReservation" when target.ValueKind == JsonValueKind.Object =>
                Journey(target, "departureTime", "arrivalTime", "Train",
                    Name(target, "departureStation"), Name(target, "arrivalStation"), reference),
            "BusReservation" when target.ValueKind == JsonValueKind.Object =>
                Journey(target, "departureTime", "arrivalTime", "Coach",
                    Name(target, "departureBusStop"), Name(target, "arrivalBusStop"), reference),
            "BoatReservation" when target.ValueKind == JsonValueKind.Object =>
                Journey(target, "departureTime", "arrivalTime", "Ferry",
                    Name(target, "departureBoatTerminal"), Name(target, "arrivalBoatTerminal"), reference),
            "LodgingReservation" =>
                Span(reservation, "checkinTime", "checkoutTime",
                    Text(target, "name") is { Length: > 0 } hotel ? $"Stay: {hotel}" : null, Where(target), reference),
            "FoodEstablishmentReservation" =>
                Span(reservation, "startTime", "endTime",
                    Text(target, "name") is { Length: > 0 } restaurant ? $"Table at {restaurant}" : null, Where(target), reference),
            "RentalCarReservation" =>
                Span(reservation, "pickupTime", "dropoffTime",
                    Name(reservation, "pickupLocation") is { Length: > 0 } pickup ? $"Car hire: {pickup}" : null,
                    reservation.TryGetProperty("pickupLocation", out var place) ? Where(place) : null, reference),
            _ => null
        };

        return detected is null ? null : (0, detected);
    }

    /// <summary>
    /// A trip from one place to another: named for how and where, starting where it departs.
    /// </summary>
    private static DetectedEvent? Journey(
        JsonElement trip, string departure, string arrival, string? how, string? from, string? to, string? reference)
    {
        if (how is null || from is null || to is null)
        {
            return null;
        }

        return Span(trip, departure, arrival, $"{how} {from} → {to}", from, reference);
    }

    /// <summary>
    /// Anything with a start and, perhaps, an end under the given names. A date with no time is
    /// a day, not midnight: the event becomes an all-day one.
    /// </summary>
    private static DetectedEvent? Span(
        JsonElement element, string startName, string endName, string? summary, string? location, string? reference)
    {
        if (summary is null
            || element.ValueKind != JsonValueKind.Object
            || Text(element, startName) is not { } start
            || !TryDate(start, out var begins))
        {
            return null;
        }

        var ends = Text(element, endName) is { } finish && TryDate(finish, out var until) && until >= begins ? until : (DateTimeOffset?)null;
        var allDay = !start.Contains('T') && !start.Contains(':');
        return new DetectedEvent(summary, begins, ends, location is { Length: > 0 } ? location : null,
            EventSource.StructuredData, allDay, reference);
    }

    /// <summary>
    /// "BA117" from an airline's IATA code and the flight number, or the number alone.
    /// </summary>
    private static string? Flight(JsonElement flight)
    {
        var number = Text(flight, "flightNumber");
        if (number is null)
        {
            return null;
        }

        var airline = flight.TryGetProperty("airline", out var carrier) && carrier.ValueKind == JsonValueKind.Object
            ? Text(carrier, "iataCode")
            : null;
        return airline is { Length: > 0 } && !number.StartsWith(airline, StringComparison.OrdinalIgnoreCase)
            ? $"Flight {airline}{number}"
            : $"Flight {number}";
    }

    /// <summary>
    /// A place's name as a traveller reads it: an airport by its code, anywhere else by its name.
    /// </summary>
    private static string? Name(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var place))
        {
            return null;
        }

        return place.ValueKind switch
        {
            JsonValueKind.String => place.GetString(),
            JsonValueKind.Object => Text(place, "iataCode") ?? Text(place, "name"),
            _ => null
        };
    }

    /// <summary>
    /// Where a place is: its name, then its street and town.
    /// </summary>
    private static string? Where(JsonElement place)
    {
        if (place.ValueKind == JsonValueKind.String)
        {
            return place.GetString();
        }

        if (place.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var address = place.TryGetProperty("address", out var postal) ? postal : default;
        var parts = address.ValueKind switch
        {
            JsonValueKind.String => [Text(place, "name"), address.GetString()],
            JsonValueKind.Object => new[] { Text(place, "name"), Text(address, "streetAddress"), Text(address, "addressLocality") },
            _ => [Text(place, "name")]
        };
        var joined = string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        return joined.Length > 0 ? joined : null;
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
