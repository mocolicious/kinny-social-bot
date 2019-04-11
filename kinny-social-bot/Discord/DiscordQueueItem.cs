using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using kinny_social_core.Api;

namespace kinny_social_bot.Discord
{
    public class DiscordQueueItem : SocialQueuedItem
    {
        public SocketCommandContext Context { get; }

        public DiscordQueueItem(SocketCommandContext context, SocialTipRequest socialTipRequest) : base(
            socialTipRequest)
        {
            Context = context;
        }

        private async Task SendMessage(SocketUser user, Embed embeded)
        {
            try
            {
                await user.SendMessageAsync(null, false, embeded);
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
                if (response.Status == TransactionStatus.Ok)
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder();
                    embedBuilder.Title = "Tip Received";
                    embedBuilder.Color = Color.DarkBlue;
                    embedBuilder.Footer = new EmbedFooterBuilder {IconUrl = "https://kinny.io/favicon.ico"};

                    embedBuilder.Description =
                        $"Received {SocialTipRequest.Amount} KIN from {Context.User.Username}. You can sign up and view your tip @ https://kinny.io.";
                    Embed embeded = embedBuilder.Build();

                    SocketUser dmUser =
                        Context.Message.MentionedUsers.FirstOrDefault(u => u.Id.ToString().Equals(SocialTipRequest.To));


                    await SendMessage(dmUser, embeded).ConfigureAwait(false);
                }

                if (response.Status != TransactionStatus.Ok)
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder();
                    embedBuilder.Title = "Tip Failed";
                    embedBuilder.Color = Color.DarkRed;
                    embedBuilder.Footer = new EmbedFooterBuilder {IconUrl = "https://kinny.io/favicon.ico"};
                    embedBuilder.Description = $"{response.Message}!";

                    await Context.User.SendMessageAsync(null, false, embedBuilder.Build());
                }
                else
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder();
                    embedBuilder.Title = "Tip Sent";
                    embedBuilder.Color = Color.DarkGreen;
                    embedBuilder.Footer = new EmbedFooterBuilder {IconUrl = "https://kinny.io/favicon.ico"};

                    embedBuilder.Description =
                        $"{SocialTipRequest.Amount} KIN sent to {SocialTipRequest.OfferParticipantsData.To.Username}!";
                    Embed embeded = embedBuilder.Build();

                    await SendMessage(Context.User, embeded).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}