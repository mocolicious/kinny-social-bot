using System;
using System.Threading.Tasks;
using kinny_social_core.Services;
using Microsoft.Extensions.Configuration;

namespace kinny_social_bot.Telegram
{
    public class TelegramCredentials
    {
        public string Token { get; }

        public TelegramCredentials(string token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }
    }

    public class TelegramCredentialGetter : ICredentialGetter<TelegramCredentials>
    {
        private readonly TelegramCredentials _credentials;

        public TelegramCredentialGetter(IConfiguration config)
        {
            _credentials = new TelegramCredentials(config["telegram_token"]);
        }

        public Task<TelegramCredentials> GetCredentials()
        {
            return Task.FromResult(_credentials);
        }
    }
}