using Microsoft.Extensions.Hosting;

namespace MyHome.Core.Devices
{
    public class DeviceStartup : BackgroundService
    {
        private readonly IDeviceController DeviceController;
        private readonly IDeviceRegistration DeviceRegistration;

        public DeviceStartup(IDeviceController deviceController, IDeviceRegistration deviceRegistration)
        {
            DeviceController = deviceController;
            DeviceRegistration = deviceRegistration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DeviceRegistration.LoadRegisteredDevice();

            while (!DeviceRegistration.IsRegistered && !stoppingToken.IsCancellationRequested)
            {
                // If the device is not registered, we will loop
                // until it gets registered.
                await Task.Delay(5000, stoppingToken);
            }
            stoppingToken.ThrowIfCancellationRequested();

            // Attempt to start device with given information.
            // if device data doesn't work, keep trying indefinetely.
            var deviceStarted = false;
            while(!deviceStarted && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DeviceController.StartAsync(stoppingToken, DeviceRegistration.Device!);
                    deviceStarted = true;
                }
                catch
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }

            await DeviceController.SendDeviceInfoAsync(stoppingToken);
            var heartbeatPt = new PeriodicTimer(TimeSpan.FromSeconds(30));
            do
            {
                // Send Heartbeat.
                await DeviceController.SendHeartBeatAsync(stoppingToken);

                // Send Device Memory.
                await DeviceController.SendMemoryAsync(stoppingToken);
            } while (await heartbeatPt.WaitForNextTickAsync(stoppingToken));
        }
    }
}
