using Newtonsoft.Json;
using System.Reflection;

namespace MyHome.Core.Devices
{
    public interface IDeviceRegistration
    {
        bool IsRegistered { get; }
        Device? Device { get; }
        void RegisterDevice(string ModelId, string DeviceId, string DpsIdScope, string DpsEntryPointName, string SymmetricKey);
        bool LoadRegisteredDevice();
    }
    public class DeviceRegistration : IDeviceRegistration
    {
        private const string DeviceFilePath = "Device.json";
        public bool IsRegistered => Device != null;

        public Device? Device { get; private set; }

        public void RegisterDevice(string ModelId, string DeviceId, string DpsIdScope, string DpsEntryPointName, string SymmetricKey)
        {
            Device = new(ModelId, DeviceId, DpsIdScope, DpsEntryPointName, SymmetricKey);
            StoreDeviceData(Device);
        }

        public bool LoadRegisteredDevice()
        {
            try
            {
                var jsonPath = GetDeviceJsonPath();
                if (!File.Exists(jsonPath))
                {
                    return false;
                }
                var data = File.ReadAllText(jsonPath);
                Device = JsonConvert.DeserializeObject<Device>(data);
                if (Device == null)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
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
    }

    public record Device(string ModelId, string DeviceId, string DpsIdScope, string DpsEndpointName, string SymmetricKey);
}
