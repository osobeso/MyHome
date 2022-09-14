namespace MyHome.Core.Devices
{
    public interface IDeviceController
    {
        Task SendDeviceInfoAsync(CancellationToken cancellationToken);
        Task SendHeartBeatAsync(CancellationToken cancellationToken);
        Task SendMemoryAsync(CancellationToken cancellationToken);
        Task StartAsync(CancellationToken cancellationToken, Device device);
        Task SendDeviceTelemetryAsync(DeviceTelemetry deviceTelemetry, CancellationToken cancellationToken);
        Task UpdatePropertyAsync(string componentId, string propertyId, object newValue, CancellationToken cancellationToken);
    }
}
