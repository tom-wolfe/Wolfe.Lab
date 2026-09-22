using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Wolfe.Lab.Mail.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMailboxWatcher()
    .AddChatClient()
    .AddEventDetector();

var app = builder.Build();

app.MapHealthChecks("/health", Only("mailbox"));
app.MapHealthChecks("/health/bridge", Only("bridge"));

await app.RunAsync();

static HealthCheckOptions Only(string check) => new()
{
    Predicate = registration => registration.Name == check,

    // The default writer says only "Healthy" or "Unhealthy", which leaves whoever is reading a
    // failed check with nowhere to go but the container's logs. The healthy body stays exactly
    // "Healthy" because Gatus matches on it; the reason is added only when there is one.
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "text/plain";
        if (report.Status == HealthStatus.Healthy)
        {
            await context.Response.WriteAsync("Healthy");
            return;
        }

        var reasons = report.Entries
            .Select(entry => entry.Value.Description)
            .Where(description => !string.IsNullOrWhiteSpace(description));

        await context.Response.WriteAsync($"Unhealthy: {string.Join("; ", reasons)}".TrimEnd(':', ' '));
    }
};
