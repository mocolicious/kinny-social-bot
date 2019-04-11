using System;
using System.Threading.Tasks;
using kinny_social_core.Api;
using RedditSharp.Things;

namespace kinny_social_bot.Reddit
{
    public class RedditQueueItem : SocialQueuedItem
    {
        public RedditSharp.Reddit Reddit { get; }
        public Comment Comment { get; }

        public RedditQueueItem(RedditSharp.Reddit reddit, Comment comment, SocialTipRequest socialTipRequest) : base(socialTipRequest)
        {
            Reddit = reddit;
            Comment = comment;
        }

        public override async Task Reply(SocialTipStatusResponse response)
        {
            try
            {
                if (response.Message.Equals("Duplicate transaction"))
                {
                    return;
                }

                if (response.Status != TransactionStatus.Ok)
                {
                    await Reddit.ComposePrivateMessageAsync($"Bleep Bloop! Tip failed send on {Comment.Subreddit}!",
                        $"Hello u/{SocialTipRequest.OfferParticipantsData.From.Username}," +
                        $"\n\tTip failed to send to u/{SocialTipRequest.OfferParticipantsData.To.Username} with error {response.Message}.",
                        SocialTipRequest.OfferParticipantsData.From.Username);
                }
                else
                {
                    await Reddit.ComposePrivateMessageAsync($"Bleep Bloop! Tip sent on {Comment.Subreddit}!",
                        $"Hello u/{SocialTipRequest.OfferParticipantsData.From.Username}, you sent {SocialTipRequest.Amount} KIN to u/{SocialTipRequest.OfferParticipantsData.To.Username}.",
                        SocialTipRequest.OfferParticipantsData.From.Username);
                }

                if (response.Status == TransactionStatus.Ok)
                {
                    await Reddit.ComposePrivateMessageAsync($"Bleep Bloop! Tip recieved on {Comment.Subreddit}!",
                        $"Hello u/{SocialTipRequest.OfferParticipantsData.To.Username}, you received {SocialTipRequest.Amount} KIN from u/{SocialTipRequest.OfferParticipantsData.From.Username}." +
                        " You can sign up and view your tip @ https://kinny.io.",
                        SocialTipRequest.OfferParticipantsData.To.Username);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
