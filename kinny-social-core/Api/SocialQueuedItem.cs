using System;
using System.Threading.Tasks;

namespace kinny_social_core.Api
{
    public abstract class SocialQueuedItem
    {
        public int TransactionId { get; set; }
        public DateTime DelayTil { get; private set; }
        public DateTime StarTime { get; set; }
        public SocialTipRequest SocialTipRequest { get; }

        public SocialQueuedItem(SocialTipRequest socialTipRequest)
        {
            SocialTipRequest = socialTipRequest;
            StarTime = DateTime.Now;
        }

        public void SetDelay()
        {
            DelayTil = DateTime.Now.AddMilliseconds(200);
        }

        public abstract Task Reply(SocialTipStatusResponse response);
    }
}
