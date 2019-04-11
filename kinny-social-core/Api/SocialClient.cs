using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Refit;

namespace kinny_social_core.Api
{
    interface ISocialClient
    {
        [Post("/social/tip/{secret}")]
        Task<SocialTipResponse> Tip([Body] SocialTipRequest request, [AliasAs("secret")] string secret);

        [Get("/social/tip/{id}/{secret}")]
        Task<SocialTipStatusResponse> Tip([AliasAs("id")] int id, [AliasAs("secret")] string secret);

        [Get("/social/user/{id}/{provider}/balance/{secret}")]
        Task<SocialTipStatusResponse> Balance([AliasAs("id")] string id, [AliasAs("provider")] string provider,
            [AliasAs("secret")] string secret);
    }

    public class SocialClient
    {
        private readonly ISocialClient _client;
        private readonly ConcurrentQueue<SocialQueuedItem> _queueItems;
        private readonly string _secret;

        public SocialClient(string secret)
        {
            _secret = secret;

            _client = RestService.For<ISocialClient>("https://prod-bot-api.kinny.io/");
            _queueItems = new ConcurrentQueue<SocialQueuedItem>();
            Timer timerQueueSendKin = new Timer(200);
            timerQueueSendKin.Elapsed += TimerQueueSendKinOnElapsed;
            timerQueueSendKin.Start();
        }

        private  async void TimerQueueSendKinOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (_queueItems.IsEmpty)
            {
                return;
            }

            SocialQueuedItem item = null;

            try
            {
                if (!_queueItems.TryDequeue(out item))
                {
                    return;
                }

                if (DateTime.Now < item.DelayTil)
                {
                    _queueItems.Enqueue(item);
                    return;
                }

                SocialTipStatusResponse response = await Tip(item.TransactionId);

                if (response.Status != TransactionStatus.Queued && response.Status != TransactionStatus.Error &&
                    response.Status != TransactionStatus.MarketPlaceApiError)
                {
                    await item.Reply(response);
                }
                else
                {
                    Enqueue(item);
                }
            }
            catch (ApiException ea)
            {
                if (ea.StatusCode != HttpStatusCode.NotFound)
                {
                    if (item != null)
                    {
                        Enqueue(item);
                    }
                }
            }
        }

        private void Enqueue(SocialQueuedItem item)
        {
            _queueItems.Enqueue(item);
            item.SetDelay();
        }

        public async Task<SocialTipResponse> Tip(SocialQueuedItem socialQueuedItem)
        {
            SocialTipResponse response = null;

            try
            {
                response = await _client.Tip(socialQueuedItem.SocialTipRequest, _secret).ConfigureAwait(false);

                if (response.Status == TransactionStatus.Queued)
                {
                    socialQueuedItem.TransactionId = response.TransactionId;
                    Enqueue(socialQueuedItem);
                }
            }
            catch (ApiException aex)
            {
                try
                {
                    response = JsonConvert.DeserializeObject<SocialTipResponse>(aex.Content);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);

                    response = new SocialTipResponse
                    {
                        Message = "Duplicate transaction",
                        Status = TransactionStatus.Error,
                        TransactionId = -1
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                response = new SocialTipResponse
                {
                    Message = ex.Message,
                    Status = TransactionStatus.Error,
                    TransactionId = -1
                };
            }

            if (response.Status != TransactionStatus.Queued)
            {
                await socialQueuedItem.Reply(
                    new SocialTipStatusResponse { Message = response.Message, Status = response.Status });
            }

            return response;
        }

        public async Task<SocialTipStatusResponse> Tip([AliasAs("id")] int id)
        {
            try
            {
                return await _client.Tip(id, _secret);
            }
            catch (ApiException e)
            {
                return JsonConvert.DeserializeObject<SocialTipStatusResponse>(e.Content);
            }
        }

        public async Task<SocialTipStatusResponse> Balance([AliasAs("id")] string id, string provider)
        {
            try
            {
                return await _client.Balance(id, provider, _secret);
            }
            catch (ApiException e)
            {
                return JsonConvert.DeserializeObject<SocialTipStatusResponse>(e.Content);
            }
        }
    }
}
