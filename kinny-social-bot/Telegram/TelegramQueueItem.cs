using System;
using System.Collections.Generic;
using System.Text;
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
                    try
                    {
                        await TelegramBot.SendTextMessageAsync(fromChatId,
                            $"Hello {SocialTipRequest.OfferParticipantsData.From.Username}," +
                            $"\n\tTip failed to send to {SocialTipRequest.OfferParticipantsData.To.Username} with error {response.Message}.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    try
                    {
                        await TelegramBot.SendTextMessageAsync(fromChatId,
                            $"Hello {SocialTipRequest.OfferParticipantsData.From.Username}, you sent {SocialTipRequest.Amount} KIN to {SocialTipRequest.OfferParticipantsData.To.Username}.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                if (response.Status == TransactionStatus.Ok)
                {
                    try
                    {
                        await TelegramBot.SendTextMessageAsync(toChatId,
                            $"Hello {SocialTipRequest.OfferParticipantsData.To.Username}, you received {SocialTipRequest.Amount} KIN from {SocialTipRequest.OfferParticipantsData.From.Username}." +
                            " You can sign up and view your tip @ https://kinny.io.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
