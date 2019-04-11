namespace kinny_social_core.Exceptions
{
    public class InvalidTipFormatException : SocialException
    {
        public InvalidTipFormatException(string tip) : base($"The provided tip '{tip}' is invalid")
        {
            //or too low (min 1)
        }

        public InvalidTipFormatException() { }
    }
}