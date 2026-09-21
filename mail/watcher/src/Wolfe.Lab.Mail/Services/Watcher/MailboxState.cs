namespace Wolfe.Lab.Mail.Services.Watcher;

/// <summary>
/// Holds info about the current mailbox state, for the health endpoint to report.
/// </summary>
internal sealed class MailboxState(TimeProvider time)
{
    private DateTimeOffset? _cycled;
    private string? _lost;

    /// <summary>
    /// The session completed another pass round the idle loop.
    /// </summary>
    public void Cycled()
    {
        _cycled = time.GetUtcNow();
        _lost = null;
    }

    /// <summary>
    /// The session ended, and why.
    /// </summary>
    public void Lost(string reason) => _lost = reason;

    /// <summary>
    /// How long since the session last turned over, or null if it never has.
    /// </summary>
    public TimeSpan? Since => _cycled is { } cycled ? time.GetUtcNow() - cycled : null;

    /// <summary>
    /// Why the session ended, when it has and has not since recovered.
    /// </summary>
    public string? Failure => _lost;
}
