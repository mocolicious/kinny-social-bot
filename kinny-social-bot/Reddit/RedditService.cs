﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using kinny_social_core.Api;
using kinny_social_core.Exceptions;
using kinny_social_core.Models;
using kinny_social_core.Parsers;
using kinny_social_core.Services;
using Microsoft.Extensions.Logging;
using RedditSharp;
using RedditSharp.Things;
using Telegram.Bot.Args;

namespace kinny_social_bot.Reddit
{
    internal class RedditService : SocialService<Comment, RedditCredentials>
    {
        private RedditSharp.Reddit _client;
        private string _userName;
        private BotWebAgent _webAgent;
        private bool _started;


        public RedditService(SocialClient apiClient,
            ILoggerFactory loggerFactory,
            ICredentialGetter<RedditCredentials> credentialGetter,
            ITipParser tipParser = null) : base(apiClient, loggerFactory.CreateLogger<RedditService>(),
            credentialGetter, "Reddit", tipParser) { }

        protected override async Task StartService(CancellationToken cancellationToken)
        {
            RedditCredentials credentials = await CredentialGetter.GetCredentials().ConfigureAwait(false);
            _userName = credentials.Username;

            do
            {
                try
                {
                    _webAgent = new BotWebAgent(credentials.Username, credentials.Password, credentials.ClientId,
                        credentials.ClientSecret, credentials.RedirectUrl);
                    _webAgent.RateLimiter = new RateLimitManager(RateLimitMode.Pace);
                    //This actually authenticates with reddit, that's why it's in a try/catch while loop
                    _client = new RedditSharp.Reddit(_webAgent, true);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await Task.Delay(TimeSpan.FromMinutes(2)).ConfigureAwait(false);
                    Logger.LogCritical(e.Message,e);
                }
            } while (true);

            StartReddit(cancellationToken);
            Logger.LogInformation($"{Platform} started on account '{_userName}'");

            await Task.Delay(-1, cancellationToken);
        }

        private async Task StartReddit(CancellationToken cancellationToken)
        {
            _started = true;
            ListingStream<Comment> stream = _client.User.GetUsernameMentions(25).Stream();
            IAsyncEnumerator<Comment> listings = _client.User.GetUsernameMentions(25).GetEnumerator(25, -1, true);
            await listings.MoveNext();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Comment comment = listings.Current;
                    await OnCommentReceived(comment).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                }


                await listings.MoveNext();
            }
        }

        private async Task OnCommentReceived(Comment comment)
        {
            if (comment == null)
            {
                return;
            }

            Logger.LogInformation($"Received message from {comment.AuthorName} ({comment.Id})");

            try
            {
                SocialTipResponse[] tips = await SendTip(comment).ConfigureAwait(false);

                if (tips.All(c => c.Status == TransactionStatus.Queued))
                {
                    await comment.SetAsReadAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Exception inner = e;

                while (inner.InnerException != null) inner = inner.InnerException;

                if (inner is SocialException sex)
                {
                    await comment.ReplyAsync($"u/{comment.AuthorName} {e.Message}");
                }

                Logger.LogError(inner.Message, inner);
            }
        }

        private void BotOnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            Logger.LogError($"Received error: {e.ApiRequestException.ErrorCode} — {e.ApiRequestException.Message}",
                e.ApiRequestException);
        }

        protected override Task<string> GetMessageText(Comment comment)
        {
            return Task.FromResult(comment.Body.Trim());
        }

        protected override Task<bool> IsTip(Comment comment)
        {
            if (comment == null)
            {
                return Task.FromResult(false);
            }

            string commentBody = comment.Body.Trim();

            return Task.FromResult(comment.Unread && commentBody.Contains(_userName) &&
                                   !comment.AuthorName.Equals(_userName));
        }

        protected override async Task<ISocialUser> GetFromUser(Comment comment)
        {
            RedditUser fromUser = await _client.GetUserAsync(comment.AuthorName).ConfigureAwait(false);
            return new SocialUser(fromUser.Name, fromUser.Id);
        }

        protected override async Task<ISocialUser[]> GetMessageMentionedUsers(Comment comment)
        {
            Thing thing = await _client.GetThingByFullnameAsync(comment.ParentId).ConfigureAwait(false);
            string toUserName = "";

            if (thing is Comment parentComment)
            {
                toUserName = parentComment.AuthorName;
            }
            else if (thing is Post parentPost)
            {
                toUserName = parentPost.AuthorName;
            }

            if (string.IsNullOrEmpty(toUserName))
            {
                return new ISocialUser[0];
            }

            RedditUser toUser = await _client.GetUserAsync(toUserName).ConfigureAwait(false);


            return new ISocialUser[]
            {
                new SocialUser(toUser.Name, toUser.Id)
            };
        }

        protected override Task<string> GetMessageId(Comment comment)
        {
            return Task.FromResult(comment.Id);
        }

        protected override Task<SocialQueuedItem> GetSocialQueueItem(SocialTipRequest request, Comment comment)
        {
            return Task.FromResult((SocialQueuedItem) new RedditQueueItem(_client, comment, request));
        }
    }
}