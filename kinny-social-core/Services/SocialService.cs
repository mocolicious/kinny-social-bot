using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using kinny_social_core.Api;
using kinny_social_core.Exceptions;
using kinny_social_core.Models;
using kinny_social_core.Parsers;
using kinny_social_core.Parsers.Impl;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace kinny_social_core.Services
{
    public abstract class SocialService<TContext, TCredentials> : BackgroundService
    {
        private readonly SocialClient _apiClient;
        private readonly ITipParser _tipParser;
        protected readonly ICredentialGetter<TCredentials> CredentialGetter;
        protected readonly ILogger Logger;
        protected readonly string Platform;

        protected SocialService(SocialClient apiClient, ILogger logger,
            ICredentialGetter<TCredentials> credentialGetter, string platform, ITipParser tipParser = null)
        {
            _apiClient = apiClient;
            Logger = logger;
            CredentialGetter = credentialGetter;
            Platform = platform;
            _tipParser = tipParser ?? new TipParser();
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"{Platform} is stopping....");
            return base.StopAsync(cancellationToken);
        }

        protected abstract Task StartService(CancellationToken cancellationToken);
        protected abstract Task<string> GetMessageText(TContext context);
        protected abstract Task<bool> IsTip(TContext context);
        protected abstract Task<ISocialUser> GetFromUser(TContext context);
        protected abstract Task<ISocialUser[]> GetMessageMentionedUsers(TContext context);
        protected abstract Task<string> GetMessageId(TContext context);
        protected abstract Task<SocialQueuedItem> GetSocialQueueItem(SocialTipRequest request, TContext context);

        protected async Task<SocialTipResponse[]> SendTip(TContext context)
        {
            if (!await IsTip(context).ConfigureAwait(false))
            {
                return new SocialTipResponse[0];
            }

            ISocialUser fromUser = await GetFromUser(context).ConfigureAwait(false);
            string messageId = await GetMessageId(context).ConfigureAwait(false);
            ISocialUser[] mentionedUsers = await GetMessageMentionedUsers(context).ConfigureAwait(false);

            if (mentionedUsers.Length == 0)
            {
                throw new NoUserMentionedException("Learn about Kinny here (https://telegram.me/kinnytip_bot?start=kinny)\r\nHow to tip Kin using Kinny:\r\n1. For (new message) direct tips: @username +amount /kinnytips\r\n2. For direct reply tips: +amount /kinnytips");
            }

            string messageText = await GetMessageText(context).ConfigureAwait(false);
            double tipAmount = _tipParser.GetTipAmount(messageText);
            List<SocialTipResponse> listOfResponses = new List<SocialTipResponse>();

            foreach (ISocialUser mentionedUser in mentionedUsers)
            {
                SocialTipRequest socialTipRequest = new SocialTipRequest
                {
                    From = fromUser.UserId,
                    To = mentionedUser.UserId,
                    Amount = tipAmount,
                    Provider = Platform,
                    OfferParticipantsData = new OfferParticipantsData
                    {
                        From = new OfferData
                        {
                            Description = $"You sent {tipAmount} KIN to {mentionedUser.Username}",
                            Title = $"Sent Tip ({Platform})",
                            Username = fromUser.Username
                        },
                        To = new OfferData
                        {
                            Description = $"{fromUser.Username} sent you {tipAmount} KIN",
                            Title = $"Received Tip ({Platform})",
                            Username = mentionedUser.Username
                        }
                    },
                    MessageId = messageId + mentionedUser.UserId
                };

                listOfResponses.Add(
                    await Tip(await GetSocialQueueItem(socialTipRequest, context).ConfigureAwait(false)));
            }

            return listOfResponses.ToArray();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"{Platform} Service is starting.");
            await StartService(cancellationToken).ConfigureAwait(false);
            Logger.LogInformation($" {Platform} Service is stopping.");
        }

        private async Task<SocialTipResponse> Tip(SocialQueuedItem item)
        {
            Logger.LogInformation(
                $"{Platform} Service sent {item.SocialTipRequest.Amount} KIN from {item.SocialTipRequest.From} to {item.SocialTipRequest.To}");
            return await _apiClient.Tip(item).ConfigureAwait(false);
        }
    }
}