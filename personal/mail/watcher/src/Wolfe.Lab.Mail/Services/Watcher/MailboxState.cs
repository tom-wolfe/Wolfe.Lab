using System.Diagnostics;

namespace Wolfe.Lab.Mail.Services.Watcher;

/// <summary>
/// Holds info about the current mailbox state, for the health endpoint to report.
/// </summary>
internal sealed class MailboxState(TimeProvider time)
{
    private DateTimeOffset? _cycled;

    /// <summary>
    /// The session completed another pass round the idle loop.
    /// </summary>
    public void Cycled()
    {
        _cycled = time.GetUtcNow();
        Failure = null;
        FailedIn = default;
    }

    /// <summary>
    /// The session ended, why, and the span it ended in.
    /// </summary>
    public void Lost(Exception failure, ActivityContext span)
    {
        Failure = failure;
        FailedIn = span;
    }

    /// <summary>
    /// How long since the session last turned over, or null if it never has.
    /// </summary>
    public TimeSpan? Since => _cycled is { } cycled ? time.GetUtcNow() - cycled : null;

    /// <summary>
    /// Why the session ended, when it has and has not since recovered.
    /// </summary>
    public Exception? Failure { get; private set; }

    /// <summary>
    /// The span <see cref="Failure"/> was recorded on, or default if none was being recorded.
    /// </summary>
    public ActivityContext FailedIn { get; private set; }
}
