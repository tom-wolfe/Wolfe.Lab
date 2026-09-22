namespace Wolfe.Lab.Build.Clients.Alerts;

/// <summary>
/// Something a person should hear about, on their phone.
/// </summary>
/// <param name="Title">What happened, in a line.</param>
/// <param name="Message">Where to look.</param>
public sealed record Alert(string Title, string Message);
