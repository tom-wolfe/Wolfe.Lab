using Microsoft.AspNetCore.Builder;
using Wolfe.Lab.Mail.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMailboxWatcher()
    .AddChatClient()
    .AddEventDetector();

var app = builder.Build();

app.MapHealthChecks("/health");

await app.RunAsync();
