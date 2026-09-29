using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Services.Ai;

/// <summary>
/// The tier that reads an event out of plain prose.
/// </summary>
internal interface IEventDetector
{
    /// <summary>
    /// Every event the text describes.
    /// </summary>
    /// <param name="body">The message's text.</param>
    /// <param name="received">When it arrived, which is what relative dates are relative to.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<IReadOnlyList<DetectedEvent>> Detect(string body, DateTimeOffset received, CancellationToken ct = default);
}