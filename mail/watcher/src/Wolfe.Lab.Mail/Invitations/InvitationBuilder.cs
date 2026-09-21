using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Invitations;

/// <summary>
/// Turns a detected event into the iCalendar text Proton Calendar will offer to add.
/// </summary>
/// <remarks>
/// <c>METHOD:REQUEST</c> is the whole point. Without it a client treats the attachment as a
/// file rather than an invitation, and nothing is offered — which is the difference between
/// this working and doing nothing at all. The MIME part has to carry the same method; see
/// <see cref="InvitationSender"/>.
/// </remarks>
internal static class InvitationBuilder
{
    internal const string Method = "REQUEST";

    /// <summary>
    /// The iCalendar body for one event.
    /// </summary>
    /// <param name="detected">The event to invite to.</param>
    /// <param name="organiser">The address the invitation comes from and goes to.</param>
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

        appointment.Attendees.Add(new Attendee($"mailto:{organiser}")
        {
            ParticipationStatus = "NEEDS-ACTION",
            Rsvp = true
        });

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
