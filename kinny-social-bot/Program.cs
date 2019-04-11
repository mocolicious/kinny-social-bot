using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kinny_social_bot.Discord;
using kinny_social_bot.Reddit;
using kinny_social_bot.Telegram;
using kinny_social_core.Api;
using kinny_social_core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;

namespace kinny_social_bot
{
    class Program
    {
        public static readonly Dictionary<string, string> DefaultConfiguration = new Dictionary<string, string>
        {
            {"discord_token", "token"},
            {"telegram_token", "token"},
            {"secret", "secret"}

        };

        static async Task Main(string[] args)
        {

            var hostBuilder = new HostBuilder();

            var host = hostBuilder
                .ConfigureHostConfiguration(ConfigureDelegate)
                .ConfigureServices(ConfigureDelegate)
                .ConfigureLogging((ConfigureLogging))
                .Build();

            await host.StartAsync().ConfigureAwait(false);

            // await Startup.RunAsync(configuration);
        }

        private static void ConfigureLogging(ILoggingBuilder logBuilder)
        {
            logBuilder.ClearProviders();
            logBuilder.AddConsole();
            logBuilder.SetMinimumLevel(LogLevel.Information);
        }

        private static void ConfigureDelegate(HostBuilderContext hostBuilder, IServiceCollection services)
        {
            var socialApiClient = new SocialClient(hostBuilder.Configuration["secret"], hostBuilder.Configuration["social_hostname"]);
            hostBuilder.HostingEnvironment.EnvironmentName = hostBuilder.Configuration["ASPNETCORE_ENVIRONMENT"];

            var wee = hostBuilder.HostingEnvironment.EnvironmentName;

            services
                .AddSingleton(socialApiClient)
                .AddSingleton<ICredentialGetter<DiscordCredentials>, DiscordCredentialGetter>()
                .AddSingleton<ICredentialGetter<TelegramCredentials>, TelegramCredentialGetter>()
                .AddSingleton<ICredentialGetter<RedditCredentials>, RedditCredentialGetter>()
                .AddHostedService<TelegramService>()
                .AddHostedService<DiscordService>()
                .AddHostedService<RedditService>();
        }
        
        private static void ConfigureDelegate(IConfigurationBuilder builder)
        {
            builder.AddInMemoryCollection(DefaultConfiguration).AddEnvironmentVariables();
        }
    }
}
