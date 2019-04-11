using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using kinny_social_core.Api;
using kinny_social_core.Exceptions;
using kinny_social_core.Models;
using kinny_social_core.Parsers;
using kinny_social_core.Parsers.Impl;
using kinny_social_core.Services;
using Microsoft.Extensions.Logging;

namespace kinny_social_bot.Discord
{
    internal class DiscordService : SocialService<SocketCommandContext, DiscordCredentials>
    {
        private DiscordSocketClient _client;
        public DiscordService(SocialClient apiClient,
            ILoggerFactory loggerFactory,
            ICredentialGetter<DiscordCredentials> credentialGetter,
            ITipParser tipParser = null) : base(apiClient,loggerFactory.CreateLogger<DiscordService>(),credentialGetter,"Discord", tipParser)
        {}
        protected override async Task StartService(CancellationToken cancellationToken)
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;

            var credentials = await CredentialGetter.GetCredentials().ConfigureAwait(false);
            _client.MessageReceived += CommandProcessing;
            await _client.LoginAsync(credentials.TokenType, credentials.Token);
            await _client.StartAsync();

            Logger.LogInformation($"{Platform} started on account '{_client.CurrentUser.Username}'");
            await Task.Delay(-1, cancellationToken);
        }
        private async Task CommandProcessing(SocketMessage sm)
        {
            if (!(sm is SocketUserMessage sum)) return;
            var context = new SocketCommandContext(_client, sum);
            if(context.User.IsBot) return;

            Logger.LogInformation($"Received message from {context.User.Username} ({context.User.Id})");
            try
            {
                await SendTip(context).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var inner = e;

                while (inner.InnerException != null)
                {
                    inner = inner.InnerException;
                }

                if (inner is SocialException sex)
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder();
                    embedBuilder.Title = "Tip Failed";
                    embedBuilder.Color = Color.DarkRed;
                    embedBuilder.Footer = new EmbedFooterBuilder { IconUrl = "https://kinny.io/favicon.ico" };
                    embedBuilder.Description = $"{inner.Message}!";

                    await context.User.SendMessageAsync(null, false, embedBuilder.Build());
                }

                Logger.LogError(inner.Message, inner);
            }
        }

        private Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    Logger.LogCritical(msg.Message, msg.Exception);
                    break;
                case LogSeverity.Error:
                    Logger.LogError(msg.Message, msg.Exception);
                    break;
                case LogSeverity.Warning:
                    Logger.LogWarning(msg.Message);
                    break;
                case LogSeverity.Info:
                    Logger.LogInformation(msg.Message);
                    break;
                case LogSeverity.Verbose:
                    Logger.LogInformation(msg.Message);
                    break;
                case LogSeverity.Debug:
                    Logger.LogDebug(msg.Message);
                    break;
            }

            return Task.CompletedTask;
        }

        protected override Task<string> GetMessageText(SocketCommandContext context)
        {
            return Task.FromResult(context.Message.Content);
        }

        protected override Task<bool> IsTip(SocketCommandContext context)
        {
            var containsSlashCommand = context.Message.Content.ToLower().Contains($"/{_client.CurrentUser.Username} ");
            var isBotMentioned = context.Message.MentionedUsers.Any(u => u.Username == _client.CurrentUser.Username);

            return Task.FromResult(containsSlashCommand || isBotMentioned);
        }

        protected override Task<ISocialUser> GetFromUser(SocketCommandContext context)
        {
             return Task.FromResult((ISocialUser)new SocialUser(context.User.Username, context.User.Id.ToString()));
        }

        protected override Task<ISocialUser[]> GetMessageMentionedUsers(SocketCommandContext context)
        {
            var mentionedUsers = context.Message.MentionedUsers.Where(u => u.Username != _client.CurrentUser.Username && u.Username != context.User.Username)
                .ToArray();
            ISocialUser[] users = new ISocialUser[mentionedUsers.Length];

            for (int i = 0; i < mentionedUsers.Length; i++)
            {
                users[i] = new SocialUser(mentionedUsers[i].Username, mentionedUsers[i].Id.ToString());
            }

            return Task.FromResult(users);
        }

        protected override Task<string> GetMessageId(SocketCommandContext context)
        {
            return Task.FromResult(context.Message.Id.ToString());
        }

        protected override Task<SocialQueuedItem> GetSocialQueueItem(SocialTipRequest request, SocketCommandContext context)
        {
            return Task.FromResult((SocialQueuedItem)new DiscordQueueItem(context, request));
        }
    }
}
