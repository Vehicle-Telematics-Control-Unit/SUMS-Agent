using FirebaseAdmin.Messaging;
using SUMS_Agent.Models;

namespace SUMS_Agent.Services
{
    public class FCMService : IFCMService
    {
        public async Task<dynamic> SendNotificationAsync(NotificationModel notificationModel)
        {
            var _message = new Message()
            {
                Notification = new Notification
                {
                    Title = notificationModel.Title,
                    Body = notificationModel.Message
                },
                Token = notificationModel.NotificationToken
            };
            try
            {
                var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(_message);
                return messageId;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }


        }
    }
}
