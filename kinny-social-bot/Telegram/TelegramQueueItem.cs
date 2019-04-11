using System;
using System.Threading.Tasks;
using kinny_social_core.Api;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace kinny_social_bot.Telegram
{
    public class TelegramQueueItem : SocialQueuedItem
    {
        public TelegramBotClient TelegramBot { get; }
        public Message Message { get; }

        public TelegramQueueItem(TelegramBotClient tg, Message message, SocialTipRequest socialTipRequest) : base(
            socialTipRequest)
        {
            TelegramBot = tg;
            Message = message;
        }

        private async Task SendMessage(ChatId chatId, string message)
        {
            try
            {
                await TelegramBot.SendTextMessageAsync(chatId, message).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override async Task Reply(SocialTipStatusResponse response)
        {
            try
            {
                ChatId fromChatId = new ChatId(SocialTipRequest.From);
                ChatId toChatId = new ChatId(SocialTipRequest.To);

                if (response.Message.Equals("Duplicate transaction"))
                {
                    return;
                }

                if (response.Status != TransactionStatus.Ok)
                {
                    await SendMessage(fromChatId,
                            $"Hello {SocialTipRequest.OfferParticipantsData.From.Username}," +
                            $"\n\tTip failed to send to {SocialTipRequest.OfferParticipantsData.To.Username} with error {response.Message}.")
                        .ConfigureAwait(false);
                }
                else
                {
                    await SendMessage(fromChatId,
                            $"Hello {SocialTipRequest.OfferParticipantsData.From.Username}, you sent {SocialTipRequest.Amount} KIN to {SocialTipRequest.OfferParticipantsData.To.Username}.")
                        .ConfigureAwait(false);
                }

                if (response.Status == TransactionStatus.Ok)
                {
                    await SendMessage(toChatId,
                        $"Hello {SocialTipRequest.OfferParticipantsData.To.Username}, you received {SocialTipRequest.Amount} KIN from {SocialTipRequest.OfferParticipantsData.From.Username}." +
                        " You can sign up and view your tip @ https://kinny.io.").ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}