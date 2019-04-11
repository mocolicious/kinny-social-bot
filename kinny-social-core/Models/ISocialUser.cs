using System;

namespace kinny_social_core.Models
{
    public class SocialUser : ISocialUser
    {
        public string Username { get; }
        public string UserId { get; }

        public SocialUser(string username, string userId)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        }

        protected bool Equals(SocialUser other)
        {
            return string.Equals(UserId, other.UserId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((SocialUser) obj);
        }

        public override int GetHashCode()
        {
            return (UserId != null ? UserId.GetHashCode() : 0);
        }
    }
    public interface ISocialUser
    {
        string Username { get; }
        string UserId { get; }
    }
}
