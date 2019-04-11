using System.Text.RegularExpressions;
using kinny_social_core.Exceptions;

namespace kinny_social_core.Parsers.Impl {
    public class TipParser : ITipParser
    {
        private static readonly Regex AmountRegex = new Regex(@"-?\d+(?:\.\d+)?");

        public double GetTipAmount(string message)
        {
            var amountMatch = AmountRegex.Match(message);
            if (amountMatch.Success && double.TryParse(amountMatch.Groups[0].Value, out double toUserAmount))
            {
                if (toUserAmount < 1)
                {
                    throw new InvalidTipAmountException(toUserAmount);
                }

                return toUserAmount;
            }

            throw new InvalidTipFormatException(message);
        }
    }
}