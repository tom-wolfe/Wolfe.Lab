namespace Wolfe.Lab.Mail.Models;

/// <summary>
/// An event found in a message.
/// </summary>
/// <param name="Summary">What to call it in a calendar.</param>
/// <param name="Start">When it begins.</param>
/// <param name="End">When it ends, when that is known.</param>
/// <param name="Location">Where, when that is known.</param>
/// <param name="Source">The evidence it came from.</param>
internal sealed record DetectedEvent(
    string Summary,
    DateTimeOffset Start,
    DateTimeOffset? End,
    string? Location,
    EventSource Source
);
