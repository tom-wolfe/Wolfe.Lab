namespace Wolfe.Lab.Mail.Models;

/// <summary>
/// An event found in a message.
/// </summary>
/// <param name="Summary">What to call it in a calendar.</param>
/// <param name="Start">When it begins.</param>
/// <param name="End">When it ends, when that is known.</param>
/// <param name="Location">Where, when that is known.</param>
/// <param name="Source">The evidence it came from.</param>
/// <param name="AllDay">Whether only the dates are known.</param>
/// <param name="Reference">The booking reference, confirmation number or PNR, when there is one.</param>
internal sealed record DetectedEvent(
    string Summary,
    DateTimeOffset Start,
    DateTimeOffset? End,
    string? Location,
    EventSource Source,
    bool AllDay = false,
    string? Reference = null
);
