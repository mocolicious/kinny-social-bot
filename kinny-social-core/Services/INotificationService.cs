using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace kinny_social_core.Services
{
    interface INotificationService
    {
        Task sendNotification(int userId);
    }
}
