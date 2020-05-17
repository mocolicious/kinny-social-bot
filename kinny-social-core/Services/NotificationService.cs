using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Messaging;
using kinny_social_core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace kinny_social_core.Services
{
    class NotificationService
    {
        protected readonly ILogger Logger;
        protected NotificationService(ILogger logger)
        {

        }

        public async Task sendNotification(int userId, string messageBody, string messageTitle)
        {
            var firebase = new FirebaseClient("https://dinosaur-facts.firebaseio.com/");
            var registrationTokens = await firebase
              .Child("Users")
              .OrderByKey()
              .OnceAsync<FirebaseTokenViewModel>();

            List<string> deviceIds = new List<string>();
            foreach (var token in registrationTokens)
            {
                deviceIds.Add(token.Object.DeviceId);
            }

            var message = new MulticastMessage()
            {
                Tokens = deviceIds,
                Data = new Dictionary<string, string>()
                 {
                     {"title", messageTitle},
                     { "body", messageBody },
                 },
            };
            var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message).ConfigureAwait(true);
        }
    }
}
