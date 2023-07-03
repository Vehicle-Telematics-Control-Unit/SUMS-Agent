using SUMS_Agent.Models;

namespace SUMS_Agent.Services
{
    public interface IFCMService
    {
        Task<dynamic> SendNotificationAsync(NotificationModel notificationModel);
    }
}
