using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Services.Ai;

/// <summary>
/// Reads events out of prose: appointments, and the bookings a journey or a trip is made of.
/// </summary>
internal sealed class ModelEventDetector(ILogger<ModelEventDetector> logger, IChatClient chat) : IEventDetector
{
    private const string Instruction = """
        You extract calendar entries from emails: appointments, meetings and events, and the
        bookings a trip is made of — flights, trains, coaches, ferries, hotel and other
        accommodation, car hire and restaurant tables.

        Return every entry the email confirms. A return journey is two entries, one each way,
        and every leg of a connection is its own.

        For each entry:
        - summary: what to call it in a calendar. A journey names how and where, e.g. "Flight
          BA117 London Heathrow to New York JFK" or "Train London Euston to Manchester
          Piccadilly". A stay names the place, e.g. "Stay: Premier Inn Leeds City Centre".
        - start and end: ISO 8601. A journey starts at departure and ends at arrival; a stay
          starts on the check-in date and ends on the check-out date. Include the time when the
          email states one, with the UTC offset of the place it happens when you know it. When
          the email gives only a date, give only the date (YYYY-MM-DD).
        - location: where it starts — the departure airport or station, the hotel.
        - reference: the booking reference, confirmation number or PNR, when there is one.

        A newsletter, a marketing email or a receipt with no booking in it is not an entry, and
        neither is anything where you would have to guess the date. Never invent a date. If
        there are no entries, return an empty list.
        """;

    /// <inheritdoc />
    public async Task<IReadOnlyList<DetectedEvent>> Detect(string body, DateTimeOffset received, CancellationToken ct = default)
    {
        var response = await chat.GetResponseAsync<Answer>(
            [
                new ChatMessage(ChatRole.System, Instruction),
                new ChatMessage(ChatRole.User, $"Today is {received:yyyy-MM-dd}. The email:\n\n{Trim(body)}")
            ],
            serializerOptions: ModelJson.Default.Options,
            useJsonSchemaResponseFormat: true,
            cancellationToken: ct
        );

        if (!response.TryGetResult(out var answer))
        {
            // The model was asked and answered in a shape that is not the schema.
            logger.LogWarning("The model's answer did not fit the schema; treating the email as having no event.");
            return [];
        }

        return Read(answer, received);
    }

    /// <summary>
    /// Turns the model's answer into events, and refuses any entry that is not a whole one.
    /// </summary>
    /// <remarks>
    /// The schema settles the SHAPE, so what is left here is sense: a summary that says
    /// something, a start that is a real date, and a date that could plausibly be the one the
    /// email is about. An entry that fills the schema with nonsense is dropped on its own; the
    /// others in the same answer still count.
    /// </remarks>
    internal static IReadOnlyList<DetectedEvent> Read(Answer answer, DateTimeOffset received) =>
        [.. (answer.Entries ?? []).Select(entry => Read(entry, received)).OfType<DetectedEvent>()];

    private static DetectedEvent? Read(Entry entry, DateTimeOffset received)
    {
        if (entry.Summary is not { Length: > 0 } summary
            || !DateTimeOffset.TryParse(entry.Start, out var begins))
        {
            return null;
        }

        // Make sure the event is in the future.
        if (begins < received.AddDays(-1))
        {
            return null;
        }

        var ends = DateTimeOffset.TryParse(entry.End, out var until) && until > begins
            ? until
            : (DateTimeOffset?)null;
        return new DetectedEvent(summary, begins, ends, entry.Location, EventSource.Model, IsDateOnly(entry.Start!), entry.Reference);
    }

    /// <summary>
    /// A start with no time in it — the model was told to give one only when the email does.
    /// </summary>
    internal static bool IsDateOnly(string value) => !value.Contains('T') && !value.Contains(':');

    /// <summary>
    /// A long marketing email is mostly footer; the model needs the top of it.
    /// </summary>
    private static string Trim(string body) => body.Length <= 6000 ? body : body[..6000];

    /// <summary>
    /// The shape the model is held to: every entry the email confirms, none when it confirms none.
    /// </summary>
    internal sealed record Answer(IReadOnlyList<Entry>? Entries);

    /// <summary>
    /// One entry. Dates stay strings so that an unparseable one is an entry dropped rather than
    /// an exception mid-deserialisation.
    /// </summary>
    internal sealed record Entry(string? Summary, string? Start, string? End, string? Location, string? Reference);
}
