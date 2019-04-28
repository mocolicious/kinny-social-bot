using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using kinny_social_core.Api;
using kinny_social_core.Exceptions;
using kinny_social_core.Models;
using kinny_social_core.Parsers;
using kinny_social_core.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace kinny_social_bot.Telegram
{
    internal class TelegramService : SocialService<Update, TelegramCredentials>
    {
        private readonly ConcurrentDictionary<string, int> _telegramUsernameToIdMap;
        private TelegramBotClient _client;
        private User _currentUser;

        public TelegramService(SocialClient apiClient,
            ILoggerFactory loggerFactory,
            ICredentialGetter<TelegramCredentials> credentialGetter,
            ITipParser tipParser = null) : base(apiClient, loggerFactory.CreateLogger<TelegramService>(),
            credentialGetter, "Telegram", tipParser)
        {
            _telegramUsernameToIdMap = new ConcurrentDictionary<string, int>();
        }

        protected override async Task StartService(CancellationToken cancellationToken)
        {
            TelegramCredentials credentials = await CredentialGetter.GetCredentials().ConfigureAwait(false);
            _client = new TelegramBotClient(credentials.Token);
            _currentUser = await _client.GetMeAsync(cancellationToken).ConfigureAwait(false);
            _client.OnReceiveError += BotOnReceiveError;
            _client.OnUpdate += BotOnOnUpdate;
            _client.StartReceiving(cancellationToken: cancellationToken);

            Logger.LogInformation($"{Platform} started on account \"{_currentUser.Username}\"");
            await Task.Delay(-1, cancellationToken);
        }

        private async void BotOnOnUpdate(object sender, UpdateEventArgs e)
        {
            try
            {
                await BotOnMessageReceived(e.Update).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, ex);
            }
        }

        private async Task BotOnMessageReceived(Update context)
        {
            if (context.Message == null)
            {
                return;
            }

            if (context.Message.From.IsBot)
            {
                return;
            }

            _telegramUsernameToIdMap.AddOrUpdate(context.Message?.From?.Username ?? context.Message.From.FirstName,
                s => context.Message.From.Id,
                (s, l) => context.Message.From.Id);

            if (context.Message.Type != MessageType.Text)
            {
                return;
            }

            Logger.LogInformation(
                $"Received message from {context.Message.From.Username ?? context.Message.From.FirstName} ({context.Message.From.Id})");

            try
            {
                var isPrivate = context.Message.Chat.Type == ChatType.Private;

                if (context.Message.Text.Equals("/start kinny") && isPrivate)
                {
                    await _client.SendTextMessageAsync(context.Message.Chat.Id, DmHelpStart).ConfigureAwait(false);
                } else if (context.Message.Text.Equals("/help") && isPrivate)
                {
                    await _client.SendTextMessageAsync(context.Message.Chat.Id, DmHelpMessage).ConfigureAwait(false);
                } else if (context.Message.Text.Equals("/kinnytips") || context.Message.Text.Equals("@kinnytip_bot"))
                {
                   await _client.SendTextMessageAsync(context.Message.Chat.Id, LearnMessage).ConfigureAwait(false);
                }
                else
                {
                    await SendTip(context).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Exception inner = e;

                while (inner.InnerException != null) inner = inner.InnerException;

                if (inner is SocialException sex)
                {
                    await _client.SendTextMessageAsync(context.Message.Chat.Id, sex.Message, ParseMode.Default, false,
                        false, context.Message.MessageId).ConfigureAwait(false);
                }

                Logger.LogError(inner.Message, inner);
            }
        }

        private void BotOnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            Logger.LogError($"Received error: {e.ApiRequestException.ErrorCode} — {e.ApiRequestException.Message}",
                e.ApiRequestException);
        }

        protected override Task<string> GetMessageText(Update context)
        {
            return Task.FromResult(context.Message.Text);
        }

        private const string DmHelpStart = "Welcome to Kinny! Kinny allows you to send and receive Kin over social media including Telegram, Discord, Reddit and Twitter. You can use Kinny to tip your friends or your favorite content creators easily and show them how much you care. Learn more about Kinny at kinny.io. To get started using Kinny in Telegram, press /help.";

        private const string DmHelpMessage =
            "How to connect your Telegram account to Kinny:\r\n\r\n1. Register on Kinny.io.\r\n2. Log in and go to Profile>Social logins.\r\n3. Click the Telegram service to add to your registered logins. You must have a valid Telegram username (navigate to Settings>Profile>Username).\r\n4. Authorize and login to Telegram when prompted.\r\n\r\nCongratulations! You can now receive and send tips to your friends within Telegram!\r\n\r\nHow to tip:\r\n\r\n1. For direct tips: @username +amount /kinnytips\r\n2. For direct reply tips: +amount /kinnytips\r\n3. For direct multi user tips: @username1 @username2 +amount /kinnytips\r\n\r\nYou can view your recent transaction history in the Kinny.io dashboard.";
        private const string LearnMessage = "Learn about Kinny here (https://telegram.me/kinnytip_bot?start=kinny)\r\nHow to tip Kin using Kinny:\r\n1. For (new message) direct tips: @username +amount /kinnytips\r\n2. For direct reply tips: +amount /kinnytips";

        protected override Task<bool> IsTip(Update context)
        {
            bool containsSlashCommand = context.Message.Text.ToLower().Contains($"/{_currentUser.FirstName}");

            bool isBotMentioned = context.Message.EntityValues != null
                                  && context.Message.EntityValues.Any(v => v.Contains(_currentUser.Username));

            return Task.FromResult(containsSlashCommand || isBotMentioned);
        }

        protected override Task<ISocialUser> GetFromUser(Update context)
        {
            return Task.FromResult((ISocialUser) new SocialUser(
                context.Message.From.Username ?? context.Message.From.FirstName, context.Message.From.Id.ToString()));
        }

        protected override Task<ISocialUser[]> GetMessageMentionedUsers(Update context)
        {
            if (context.Message.ReplyToMessage != null)
            {
                return Task.FromResult(new ISocialUser[]
                {
                    new SocialUser(
                        context.Message.ReplyToMessage.From.Username ?? context.Message.ReplyToMessage.From.FirstName,
                        context.Message.ReplyToMessage.From.Id.ToString())
                });
            }

            string fromUserName = context.Message.From.Username ?? context.Message.From.FirstName;

            string[] mentionedUsers = context.Message.EntityValues
                .Where(v => !v.Contains(_currentUser.Username) && !v.Contains("/kinnytips") &&
                            !v.Contains(fromUserName)).Select(v => v.Replace("@", ""))
                .ToArray();

            HashSet<ISocialUser> users = new HashSet<ISocialUser>();

            for (int i = 0; i < mentionedUsers.Length; i++)
            {
                if (_telegramUsernameToIdMap.TryGetValue(mentionedUsers[i], out int toUserId))
                {
                    users.Add(new SocialUser(mentionedUsers[i], toUserId.ToString()));
                }
            }

            return Task.FromResult(users.ToArray());
        }

        protected override Task<string> GetMessageId(Update context)
        {
            return Task.FromResult(context.Id.ToString());
        }

        protected override Task<SocialQueuedItem> GetSocialQueueItem(SocialTipRequest request, Update message)
        {
            return Task.FromResult((SocialQueuedItem) new TelegramQueueItem(_client, message.Message, request));
        }
    }
}
