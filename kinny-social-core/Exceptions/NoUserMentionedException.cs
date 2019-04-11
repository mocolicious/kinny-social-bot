namespace kinny_social_core.Exceptions {
    public class NoUserMentionedException : SocialException
    {
        public NoUserMentionedException() { }
        public NoUserMentionedException(string message) : base(message) { }
    }
}