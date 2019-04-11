using System;

namespace kinny_social_core.Exceptions
{
    public class SocialException : Exception
    {
        public SocialException() { }
        public SocialException(string message) : base(message) { }
    }
}