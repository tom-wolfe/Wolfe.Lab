using Microsoft.Extensions.Hosting;
using Wolfe.Lab.Mail.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMailboxWatcher()
    .AddChatClient()
    .AddEventDetector();

await builder.Build().RunAsync();
