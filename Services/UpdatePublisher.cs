using Microsoft.EntityFrameworkCore;
using SUMS_Agent.Models;

namespace SUMS_Agent.Services
{
    public class UpdatePublisherService : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly TCUContext tcuContext;
        private readonly IConfiguration _config;
        private readonly IFCMService _FCMService;
        public UpdatePublisherService(IConfiguration configuration, IFCMService fCMService)
        {
            _config = configuration;
            var connectionString = _config.GetConnectionString("TcuServerConnection");
            var options = new DbContextOptionsBuilder<TCUContext>()
               .UseNpgsql(connectionString)
               .Options;
            tcuContext = new TCUContext(options);
            _FCMService = fCMService;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {

            _timer = new Timer(PublishUpdates, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private void PublishUpdates(object? state)
        {
            var now = DateTime.Now;
            // get features that are scheduled to publish
            var featuresToPublish = (from _feature in tcuContext.Features
                                     where _feature.IsActive == false
                                     && _feature.ReleaseDate <= now
                                     select _feature).ToList();

            Console.WriteLine("Found " + featuresToPublish.Count.ToString() + " features");

            // get TCU's that must send to them publish
            foreach (Feature featureToPublish in featuresToPublish)
            {
                var relatedModelsId = (from _modelFeature in tcuContext.ModelsFeatures
                                       where _modelFeature.FeatureId == featureToPublish.FeatureId
                                       select _modelFeature.ModelId).ToList();

                var tcuToPublishFor = (from _tcu in tcuContext.Tcus
                                       where relatedModelsId.Contains(_tcu.ModelId)
                                       select _tcu.TcuId).ToList();

                // get all the mobile devices connected to that TCUs
                var mobileDevicesIds = (from _deviceTCU in tcuContext.DevicesTcus
                                        where tcuToPublishFor.Contains(_deviceTCU.TcuId)
                                        select _deviceTCU.DeviceId);

                var devices = (from _device in tcuContext.Devices
                               where mobileDevicesIds.Contains(_device.DeviceId)
                               select _device).ToList();

                // send notifcations regarding the update to all devices
                foreach (Device device in devices)
                {
                    var deviceNotificationToken = device.NotificationToken;
                    if (deviceNotificationToken == null)
                        continue;
                    // send notification
                    var messageId = _FCMService.SendNotificationAsync(
                        new NotificationModel
                        {
                            Title = featureToPublish.FeatureName + " is now available !!!",
                            Message = featureToPublish.Description,
                            NotificationToken = device.NotificationToken
                        });

                }
                Console.WriteLine("Sent to" + devices.Count + " device");
                featureToPublish.IsActive = true;
                tcuContext.SaveChanges();
                Console.WriteLine(featureToPublish.FeatureName + "has been published");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
