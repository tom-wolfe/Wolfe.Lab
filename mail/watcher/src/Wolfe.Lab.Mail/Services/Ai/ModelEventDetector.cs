using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Services.Ai;

/// <summary>
/// Reads an event out of prose.
/// </summary>
internal sealed class ModelEventDetector(ILogger<ModelEventDetector> logger, IChatClient chat) : IEventDetector
{
    private const string Instruction = """
        You extract calendar events from emails.

        An event is a specific appointment, booking, reservation or meeting with a date AND a
        time. A newsletter, a marketing email or a receipt with no appointment is not one, and
        neither is anything where you would have to guess the date.

        Never invent a date. If the email does not state one, there is no event.
        """;

    /// <inheritdoc />
    public async Task<DetectedEvent?> Detect(string body, DateTimeOffset received, CancellationToken ct = default)
    {
        ChatResponse<Answer> response;
        try
        {
            response = await chat.GetResponseAsync<Answer>(
                [
                    new ChatMessage(ChatRole.System, Instruction),
                    new ChatMessage(ChatRole.User, $"Today is {received:yyyy-MM-dd}. The email:\n\n{Trim(body)}")
                ],
                serializerOptions: ModelJson.Default.Options,
                useJsonSchemaResponseFormat: true,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // A model that is down, slow or talking nonsense must not cost the message: leaving
            // it unread means the next pass sees it again, where a throw here would not.
            logger.LogWarning(ex, "The model did not answer; leaving this message for the next pass.");
            return null;
        }

        return response.TryGetResult(out var answer) ? Read(answer, received) : null;
    }

    /// <summary>
    /// Turns the model's answer into an event, and refuses anything that is not a whole one.
    /// </summary>
    /// <remarks>
    /// The schema settles the SHAPE, so what is left here is sense: a summary that says
    /// something, a start that is a real instant, and a date that could plausibly be the one the
    /// email is about. A model that fills the schema with nonsense is treated exactly like a
    /// model that found nothing — the message is simply left alone.
    /// </remarks>
    internal static DetectedEvent? Read(Answer answer, DateTimeOffset received)
    {
        if (!answer.Event
            || answer.Summary is not { Length: > 0 } summary
            || !DateTimeOffset.TryParse(answer.Start, out var begins))
        {
            return null;
        }

        // Make sure the event is in the future.
        if (begins < received.AddDays(-1))
        {
            return null;
        }

        var ends = DateTimeOffset.TryParse(answer.End, out var until) && until > begins
            ? until
            : (DateTimeOffset?)null;
        return new DetectedEvent(summary, begins, ends, answer.Location, EventSource.Model);
    }

    /// <summary>
    /// A long marketing email is mostly footer; the model needs the top of it.
    /// </summary>
    private static string Trim(string body) => body.Length <= 6000 ? body : body[..6000];

    /// <summary>
    /// The shape the model is held to. Dates stay strings so that an unparseable one is a
    /// message left alone rather than an exception mid-deserialisation.
    /// </summary>
    internal sealed record Answer(bool Event, string? Summary, string? Start, string? End, string? Location);
}
