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

            while (DeviceRegistration.RegistrationState == DeviceRegistrationState.Pending 
                && !stoppingToken.IsCancellationRequested)
            {
                // If the device is not registered, we will loop
                // until it gets registered.
                await Task.Delay(5000, stoppingToken);
            }
            stoppingToken.ThrowIfCancellationRequested();

            // Attempt to start device with given information.
            // if device data doesn't work, keep trying indefinetely.
            while(DeviceRegistration.RegistrationState != DeviceRegistrationState.Applied
                && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DeviceController.LoadDeviceAsync(stoppingToken, DeviceRegistration.Device!);
                    DeviceRegistration.SetApplied();
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
                await CheckForDeviceChangesAsync();
                // Send Heartbeat.
                await DeviceController.SendHeartBeatAsync(stoppingToken);

                // Send Device Memory.
                await DeviceController.SendMemoryAsync(stoppingToken);
            } while (await heartbeatPt.WaitForNextTickAsync(stoppingToken));
        }

        private async Task CheckForDeviceChangesAsync()
        {
            if (DeviceRegistration.RegistrationState == DeviceRegistrationState.Changed)
            {
                await DeviceController.LoadDeviceAsync(default, DeviceRegistration.Device!);
                DeviceRegistration.SetApplied();
            }
        }
    }
}
