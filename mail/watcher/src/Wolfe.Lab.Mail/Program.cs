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
app.MapHealthChecks("/health", new HealthCheckOptions
{
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
});

await app.RunAsync();
