namespace kinny_social_core.Exceptions {
    public class InvalidTipAmountException : SocialException
    {
        public InvalidTipAmountException(double amount) : base($"The provided tip amount '{amount}' is too low (min 1)")
        {

        }

        public InvalidTipAmountException() { }
    }
}