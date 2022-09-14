using MyHome.Core.Devices.Constants;
using MyHome.Shared;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System.Diagnostics;

namespace MyHome.Core.Devices
{
    public class DeviceController : IDeviceController
    {
        private readonly IMqttClient MqttClient;
        private readonly ILogger Log;
        private readonly SemaphoreSlim TelemetrySemaphore = new(1, 1);
        private DeviceClient? Device;
        private Device? Data;
        public DeviceController(IMqttClient client, ILogger<DeviceController> logger)
        {
            // Get config values
            MqttClient = client;
            Log = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken, Device data)
        {
            if (Device != null)
            {
                throw new Exception("Device already initialized.");
            }
            Data = data;
            Device = await SetupDeviceClientAsync(cancellationToken);
            Device.SetConnectionStatusChangesHandler(async (status, reason) =>
            {

                await Task.CompletedTask;
                /* TODO
                if (status == ConnectionStatus.Connected)
                {
                    await GetWritablePropertiesAndHandleChangesAsync();
                }
                */
            });
            await Device.SetMethodHandlerAsync(Commands.FlickSwitch, HandleBulbSwitchCommandAsync, Device, cancellationToken);
        }

        public async Task SendDeviceInfoAsync(CancellationToken cancellationToken)
        {
            var deviceInfo = PnpConvention.CreateComponentPropertyPatch(
                Components.DeviceInformation,
                new Dictionary<string, object>
                {
                    { DeviceInformationFields.Manufacturer, "manufacturer" },
                    { DeviceInformationFields.Model, "modelo" },
                    { DeviceInformationFields.SoftwareVersion, "0.0.1" },
                    { DeviceInformationFields.OperatingSystem, "raspbian" },
                    { DeviceInformationFields.OSVersion, "10" },
                    { DeviceInformationFields.ProcessorArchitecture, "linux-x86" },
                    { DeviceInformationFields.Storage, 256 },
                    { DeviceInformationFields.Memory, 1024 },
                    { DeviceInformationFields.MachineId, "MachineID" }
                });
            await Device!.UpdateReportedPropertiesAsync(deviceInfo, cancellationToken);
            Log.LogDebug($"Updated Device Information");
        }

        public async Task UpdatePropertyAsync(string componentId, string propertyId, object newValue, CancellationToken cancellationToken)
        {
            var deviceProp = PnpConvention.CreateComponentPropertyPatch(componentId, new Dictionary<string, object>
            {
                { propertyId, newValue }
            });
            await Device!.UpdateReportedPropertiesAsync(deviceProp, cancellationToken);
            Log.LogInformation("Updated property {propertyName} on component {componentId}", propertyId, componentId);
        }

        public async Task SendDeviceTelemetryAsync(DeviceTelemetry deviceTelemetry, CancellationToken cancellationToken)
        {
            if (Device == null)
            {
                // Device hasn't initialized yet. Ignore telemetry.
                return;
            }
#pragma warning disable CS8604 // Possible null reference argument.
            using Message msg = PnpConvention.CreateMessage(deviceTelemetry.Properties, componentName: deviceTelemetry.ComponentId);
#pragma warning restore CS8604 // Possible null reference argument.
            try
            {
                await TelemetrySemaphore.WaitAsync(cancellationToken);
                await Device!.SendEventAsync(msg, cancellationToken);
                Log.LogInformation("Device Telemetry Sent from Component: {ComponentId}", deviceTelemetry.ComponentId);
            }
            finally
            {
                TelemetrySemaphore.Release();
            }
        }

        public async Task SendHeartBeatAsync(CancellationToken cancellationToken)
        {
            if (Device == null)
            {
                return;
            }
            var telemetry = new Dictionary<string, object>
            {
                {TelemetryEvents.HeartBeat, 1},
            };
            using Message msg = PnpConvention.CreateMessage(telemetry);
            await Device!.SendEventAsync(msg, cancellationToken);
        }

        public async Task SendMemoryAsync(CancellationToken cancellationToken)
        {
            if (Device == null)
            {
                return;
            }
            var currentMemory = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
            var telemetry = new Dictionary<string, object>
            {
                {TelemetryEvents.WorkingSet, currentMemory},
            };
            using Message msg = PnpConvention.CreateMessage(telemetry);
            await Device!.SendEventAsync(msg, cancellationToken);
        }

        private async Task<DeviceClient> SetupDeviceClientAsync(CancellationToken cancellationToken)
        {
            SecurityProviderSymmetricKey symmetricKeyProvider = new(Data!.DeviceId, Data!.SymmetricKey, null);
            ProvisioningTransportHandlerMqtt mqttTransportHandler = new();
            ProvisioningDeviceClient pdc = ProvisioningDeviceClient.Create(Data!.DpsEndpointName, Data!.DpsIdScope, symmetricKeyProvider, mqttTransportHandler);

            var pnpPayload = new ProvisioningRegistrationAdditionalData
            {
                JsonData = Microsoft.Azure.Devices.Provisioning.Client.PlugAndPlay.PnpConvention.CreateDpsPayload(Data!.ModelId),
            };
            var result = await pdc.RegisterAsync(pnpPayload, cancellationToken);
            var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(result.DeviceId, Data!.SymmetricKey);
            var options = new ClientOptions
            {
                ModelId = Data!.ModelId
            };
            var deviceClient = DeviceClient.Create(result.AssignedHub, authMethod, options);
            return deviceClient;
        }

        private async Task<MethodResponse> HandleBulbSwitchCommandAsync(MethodRequest request, object userContext)
        {
            try
            {
                if (MqttClient.IsConnected)
                {
                    Log.LogInformation("Command Received: Lightbulb Switch");
                    var messageBuilder = new MqttFactory().CreateApplicationMessageBuilder();
                    messageBuilder.WithTopic(TopicNames.MyHomeToLightBulb);
                    var publishResult = await MqttClient.PublishAsync(messageBuilder.Build(), CancellationToken.None);
                    if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
                    {
                        Log.LogError("Connection succeeded but could not send message to broker. Reason Code: {ReasonCode}", publishResult.ReasonCode);
                    }
                    Log.LogDebug("Command Button Message sent to MQTT");
                }else
                {
                    Log.LogWarning("Attempt to send light bulb command message failed because the mqtt client is not connected.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError("Error trying to send command to MQTT broker: {Message}", ex.Message);
                return new MethodResponse((int)StatusCode.Error);
            }
            return new MethodResponse((int)StatusCode.Completed);
        }

        /** TODO
        private async Task GetWritablePropertiesAndHandleChangesAsync()
        {
            var device = Device!;
            Twin twin = await device.GetTwinAsync();
            // _logger.LogInformation($"Device retrieving twin values on CONNECT: {twin.ToJson()}");

            TwinCollection twinCollection = twin.Properties.Desired;
            foreach (KeyValuePair<string, object> property in twinCollection)
            {
                var propertyValue = JsonConvert.SerializeObject(property);
                Log.LogTrace("Property Read: {propertyValue}", propertyValue);
            }


            // Check if the writable property version is outdated on the local side.
            // For the purpose of this sample, we'll only check the writable property versions between local and server
            // side without comparing the property values.
            /*if (serverWritablePropertiesVersion > s_localWritablePropertiesVersion)
            {
                _logger.LogInformation($"The writable property version cached on local is changing " +
                    $"from {s_localWritablePropertiesVersion} to {serverWritablePropertiesVersion}.");

                foreach (KeyValuePair<string, object> propertyUpdate in twinCollection)
                {
                    string componentName = propertyUpdate.Key;
                    switch (componentName)
                    {
                        case Thermostat1:
                        case Thermostat2:
                            // This will be called when a device client gets initialized and the _temperature dictionary is still empty.
                            if (!_temperature.TryGetValue(componentName, out double value))
                            {
                                _temperature[componentName] = 21d; // The default temperature value is 21°C.
                            }
                            await TargetTemperatureUpdateCallbackAsync(twinCollection, componentName);
                            break;

                        default:
                            _logger.LogWarning($"Property: Received an unrecognized property update from service:" +
                                $"\n[ {propertyUpdate.Key}: {propertyUpdate.Value} ].");
                            break;
                    }
                }

                _logger.LogInformation($"The writable property version on local is currently {s_localWritablePropertiesVersion}.");
            }
        }*/
    }
}
