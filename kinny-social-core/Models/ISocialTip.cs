using System;

namespace kinny_social_core.Models
{
    public class SocialTip : ISocialTip
    {
        public ISocialUser FromUser { get; }
        public ISocialUser[] ToUsers { get; }
        public string SocialPlatform { get; }
        public int Amount { get; }

        public SocialTip(string socialPlatform, ISocialUser fromUser, int amount, params ISocialUser[] users)
        {
            SocialPlatform = socialPlatform ?? throw new ArgumentNullException(nameof(socialPlatform));
            FromUser = fromUser ?? throw new ArgumentNullException(nameof(fromUser));
            Amount = amount;
            ToUsers = users ?? throw new ArgumentNullException(nameof(users));
        }
    }

    public interface ISocialTip
    {
        ISocialUser FromUser { get; }
        ISocialUser[] ToUsers { get; }
        string SocialPlatform { get; }
        int Amount { get; }
    }
}
