using System;
using System.Threading.Tasks;
using Discord;
using kinny_social_core.Services;
using Microsoft.Extensions.Configuration;

namespace kinny_social_bot.Discord
{
    public class DiscordCredentials
    {
        public string Token { get; }
        public TokenType TokenType { get; }

        public DiscordCredentials(string token, TokenType tokenType = TokenType.Bot)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            TokenType = tokenType;
        }
    }

    public class DiscordCredentialGetter : ICredentialGetter<DiscordCredentials>
    {
        private readonly DiscordCredentials _credentials;

        public DiscordCredentialGetter(IConfiguration config)
        {
            _credentials = new DiscordCredentials(config["discord_token"]);
        }

        public Task<DiscordCredentials> GetCredentials()
        {
            return Task.FromResult(_credentials);
        }
    }
}