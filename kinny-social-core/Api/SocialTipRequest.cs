namespace kinny_social_core.Api
{
    public class SocialTipResponse
    {
        public int TransactionId { get; set; }
        public TransactionStatus Status { get; set; }
        public string Message { get; set; }
    }

    public class SocialTipRequest
    {
        public string MessageId { get; set; }
        public string Provider { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public double Amount { get; set; }
        public OfferParticipantsData OfferParticipantsData { get; set; }
    }

    public class OfferParticipantsData
    {
        public OfferData From { get; set; }
        public OfferData To { get; set; }
    }

    public class OfferData
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Username { get; set; }
    }

    public class SocialTipStatusResponse
    {
        public TransactionStatus Status { get; set; }
        public string TxHash { get; set; }
        public string Message { get; set; }
    }

    public enum TransactionStatus
    {
        Ok,
        Queued,
        NotEnoughKin,
        NoKinAsset,
        MarketPlaceApiError,
        Error
    }
}