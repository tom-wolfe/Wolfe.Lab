using System.Diagnostics;
using System.Diagnostics.Metrics;
using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Diagnostics;

/// <summary>
/// What the watcher reports about its own work, beside what MailKit, the HTTP client and the
/// model client report about theirs.
/// </summary>
/// <remarks>
/// Nothing here carries what a message says — no subject, no sender, no text — only which
/// message it was and what came of it. The model client is held to the same line: it records
/// the call, its duration and its tokens, not the prompt.
/// </remarks>
internal static class MailTelemetry
{
    /// <summary>
    /// The name the watcher's spans and metrics go out under, the model calls' included.
    /// </summary>
    public const string Name = "Wolfe.Lab.Mail";

    public static readonly ActivitySource Source = new(Name);

    private static readonly Meter Meter = new(Name);

    /// <summary>
    /// Every message read, by what came of it (<see cref="Outcome"/>).
    /// </summary>
    public static readonly Counter<long> Messages =
        Meter.CreateCounter<long>("mail.messages", "{message}", "Messages the watcher has read, by what came of them.");

    /// <summary>
    /// Every invitation sent, by where its event was found.
    /// </summary>
    public static readonly Counter<long> Invitations =
        Meter.CreateCounter<long>("mail.invitations", "{invitation}", "Invitations sent, by where the event was found.");

    /// <summary>
    /// The tag a message's outcome goes under, on its span and on <see cref="Messages"/>.
    /// </summary>
    public const string OutcomeTag = "mail.outcome";

    /// <summary>
    /// The tag an event's evidence goes under, on the invitation's span and on
    /// <see cref="Invitations"/>.
    /// </summary>
    public const string SourceTag = "mail.event.source";

    /// <summary>
    /// How an event's evidence is spelled on a tag.
    /// </summary>
    public static string Tag(EventSource source) => source switch
    {
        EventSource.StructuredData => "structured",
        EventSource.Model => "model",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    /// <summary>
    /// What came of a message.
    /// </summary>
    public static class Outcome
    {
        /// <summary>
        /// It was an invitation already, so it was left to the mail client.
        /// </summary>
        public const string Invitation = "invitation";

        /// <summary>
        /// Its own structured data named an event.
        /// </summary>
        public const string Structured = "structured";

        /// <summary>
        /// The model read an event out of its text.
        /// </summary>
        public const string Model = "model";

        /// <summary>
        /// Nothing in it was an event.
        /// </summary>
        public const string None = "none";
    }
}
