using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Services.Ai;

/// <summary>
/// The tier that reads an event out of plain prose.
/// </summary>
internal interface IEventDetector
{
    /// <summary>
    /// The event the text describes, or null when it describes none.
    /// </summary>
    /// <param name="body">The message's text.</param>
    /// <param name="received">When it arrived, which is what relative dates are relative to.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<DetectedEvent?> Detect(string body, DateTimeOffset received, CancellationToken ct = default);
}