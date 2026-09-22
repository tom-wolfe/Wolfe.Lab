using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Invitations;

/// <summary>
/// Turns a detected event into the iCalendar text Proton Calendar will offer to add.
/// </summary>
internal static class InvitationBuilder
{
    internal const string Method = "PUBLISH";

    /// <summary>
    /// The iCalendar body for one event.
    /// </summary>
    /// <param name="detected">The event to invite to.</param>
    /// <param name="organiser">The address the event is published from.</param>
    /// <param name="uid">A stable identity for the event, so a second send updates rather than duplicates.</param>
    /// <param name="stamp">When the invitation was produced.</param>
    public static string Build(DetectedEvent detected, string organiser, string uid, DateTimeOffset stamp)
    {
        var appointment = new CalendarEvent
        {
            Uid = uid,
            Summary = detected.Summary,
            Location = detected.Location,
            DtStamp = new CalDateTime(stamp.UtcDateTime, "UTC"),
            Start = new CalDateTime(detected.Start.UtcDateTime, "UTC"),
            // An event with no stated end is an hour long. Guessing is better than a
            // zero-length event, which some clients render as a point and some hide.
            End = new CalDateTime((detected.End ?? detected.Start.AddHours(1)).UtcDateTime, "UTC"),
            Organizer = new Organizer($"mailto:{organiser}")
        };

        // No ATTENDEE. Under PUBLISH there is nobody to RSVP, and listing the recipient as an
        // attendee of an event they also organise is what stops a client offering to add it.

        var calendar = new Calendar { Method = Method };
        calendar.Events.Add(appointment);

        // A library returning null here is a packaging fault, not anything a message did.
        return new CalendarSerializer().SerializeToString(calendar)
            ?? throw new InvalidOperationException("The calendar serialised to nothing.");
    }

    /// <summary>
    /// A UID derived from the message it came from, so the same mail processed twice — a
    /// re-send after a crash, say — updates the event rather than making a second one.
    /// </summary>
    /// <param name="messageId">The source message's Message-ID.</param>
    public static string UidFor(string messageId) =>
        $"{messageId.Trim('<', '>', ' ')}.lab";
}
