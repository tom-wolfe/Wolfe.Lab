namespace Wolfe.Lab.Mail.Models;

/// <summary>
/// Which kind of evidence an event came from. Recorded because the tiers are not equally
/// trustworthy: structured data is a fact the sender published, and prose is a reading.
/// </summary>
internal enum EventSource
{
    /// <summary>
    /// schema.org JSON-LD in the message's own HTML.
    /// </summary>
    StructuredData,

    /// <summary>
    /// A model's reading of prose.
    /// </summary>
    Model
}