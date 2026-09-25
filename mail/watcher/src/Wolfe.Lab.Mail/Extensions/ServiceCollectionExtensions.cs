using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using Wolfe.Lab.Mail.Invitations;
using Wolfe.Lab.Mail.Services.Ai;
using Wolfe.Lab.Mail.Services.Watcher;

namespace Wolfe.Lab.Mail.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the mail watcher loop.
        /// </summary>
        public IServiceCollection AddMailboxWatcher()
        {
            services.AddOptions<MailboxWatcherOptions>()
                .BindConfiguration(MailboxWatcherOptions.Section)
                .PostConfigure(options => options.Recipient =
                    options.Recipient is { Length: > 0 } recipient ? recipient : options.Username)
                .Validate(options => options.Username is { Length: > 0 }, "Username is required")
                .Validate(options => options.Password is { Length: > 0 }, "Password is required")
                .ValidateOnStart();

            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<MailboxState>();
            services.AddHealthChecks()
                .AddCheck<MailboxHealthCheck>("mailbox")
                .AddCheck<BridgeHealthCheck>("bridge");

            services.AddSingleton<InvitationSender>();
            services.AddSingleton<MailboxWatcher>();
            services.AddHostedService<MailboxWatcherHost>();
            return services;
        }

        /// <summary>
        /// Adds the <see cref="IChatClient"/>.
        /// </summary>
        public IServiceCollection AddChatClient()
        {
            services.AddOptions<ModelOptions>()
                .BindConfiguration(ModelOptions.Section)
                .Validate(options => options.Endpoint is { Length: > 0 }, "Endpoint is required")
                .Validate(options => options.Name is { Length: > 0 }, "Name is required")
                .ValidateOnStart();

            services.AddSingleton(provider =>
            {
                var model = provider.GetRequiredService<IOptions<ModelOptions>>().Value;
                return new OpenAIClient(
                    new ApiKeyCredential("ollama"),
                    new OpenAIClientOptions
                    {
                        Endpoint = new Uri($"{model.Endpoint.TrimEnd('/')}/v1"),
                        NetworkTimeout = TimeSpan.FromMinutes(3)
                    });
            });

            services.AddSingleton<IChatClient>(provider =>
                provider.GetRequiredService<OpenAIClient>()
                    .GetChatClient(provider.GetRequiredService<IOptions<ModelOptions>>().Value.Name)
                    .AsIChatClient());

            return services;
        }

        /// <summary>
        /// Adds the event detector.
        /// </summary>
        public IServiceCollection AddEventDetector() => services.AddSingleton<IEventDetector, ModelEventDetector>();
    }
}
