using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Reflection;

namespace MyHome.Core.Devices
{
    public interface IDeviceRegistration
    {
        DeviceRegistrationState RegistrationState { get; }
        Device? Device { get; }
        void RegisterDevice(string ModelId, string DeviceId, string DpsIdScope, string DpsEntryPointName, string SymmetricKey);
        void LoadRegisteredDevice();
        void SetApplied();
        void ClearDevice();
    }
    public class DeviceRegistration : IDeviceRegistration
    {
        private const string DeviceFilePath = "Device.json";
        public DeviceRegistrationState RegistrationState { get; private set; }

        public Device? Device { get; private set; }
        private readonly ILogger _logger;

        public DeviceRegistration(ILogger<DeviceRegistration> logger)
        {
            _logger = logger;
        }

        public void RegisterDevice(string ModelId, string DeviceId, string DpsIdScope, string DpsEntryPointName, string SymmetricKey)
        {
            Device = new(ModelId, DeviceId, DpsIdScope, DpsEntryPointName, SymmetricKey);
            StoreDeviceData(Device);
            RegistrationState = DeviceRegistrationState.Changed;
        }

        public void LoadRegisteredDevice()
        {
            try
            {
                var jsonPath = GetDeviceJsonPath();
                if (!File.Exists(jsonPath))
                {
                    return;
                }
                var data = File.ReadAllText(jsonPath);
                Device = JsonConvert.DeserializeObject<Device>(data);
                if (Device == null)
                {
                    return;
                }

                RegistrationState = DeviceRegistrationState.Applied;
            }
            catch
            {
                RegistrationState = DeviceRegistrationState.Pending;
                return;
            }
        }

        private void StoreDeviceData(Device device)
        {
            var jsonPath = GetDeviceJsonPath();
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(device));
        }

        private string GetDeviceJsonPath()
        {
            var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dataPath = Path.Combine(rootPath!, DeviceFilePath);
            return dataPath;
        }

        public void SetApplied()
        {
            RegistrationState = DeviceRegistrationState.Applied;
            _logger.LogInformation("Device changes have been applied.");
        }

        public void ClearDevice()
        {
            RegistrationState = DeviceRegistrationState.Pending;
            Device = null;
        }
    }

    public record Device(string ModelId, string DeviceId, string DpsIdScope, string DpsEndpointName, string SymmetricKey);
}

public enum DeviceRegistrationState
{
    Pending,
    Changed,
    Applied,
}
