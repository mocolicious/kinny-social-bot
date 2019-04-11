using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using kinny_social_bot.Discord;
using kinny_social_core.Services;
using Microsoft.Extensions.Configuration;

namespace kinny_social_bot.Reddit
{
    public class RedditCredentials
    {
        public string Username { get; }
        public string Password { get; }
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string RedirectUrl { get; }

        public RedditCredentials(string username, string password, string clientId, string clientSecret,
            string redirectUrl)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            ClientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            RedirectUrl = redirectUrl ?? throw new ArgumentNullException(nameof(redirectUrl));
        }
    }

    public class RedditCredentialGetter : ICredentialGetter<RedditCredentials>
    {
        private readonly RedditCredentials _credentials;
        public RedditCredentialGetter(IConfiguration config)
        {
            var username = config["reddit_username"];
            var password = config["reddit_password"];
            var clientId = config["reddit_clientId"];
            var clientSecret = config["reddit_clientsecret"];
            var redirectUrl = config["reddit_redirecturl"];

            _credentials = new RedditCredentials(username,password,clientId,clientSecret,redirectUrl);
        }

        public Task<RedditCredentials> GetCredentials()
        {
            return Task.FromResult(_credentials);
        }
    }
}
