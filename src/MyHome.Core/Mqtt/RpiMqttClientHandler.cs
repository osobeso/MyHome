using MyHome.Core.Devices;
using MyHome.Core.Devices.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using MyHome.Shared;

namespace MyHome.Core.Mqtt
{
    public class RpiMqttClientHandler : MqttClientHandler
    {
        private readonly IDeviceController DeviceController;
        public RpiMqttClientHandler(IDeviceController controller, IMqttClient client, IConfiguration config, ILogger<MqttClientHandler> logger)
            : base(client, config, logger)
        {
            DeviceController = controller;
        }

        protected override string Topic => "#"; // To subscribe to all topics.

        protected override async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            if (arg.ApplicationMessage.Topic == TopicNames.ThermostatToMyHome)
            {
                var payload = ParsePayload<ThermostatPayload>(arg.ApplicationMessage);
                var telemetry = new DeviceTelemetry(payload.ComponentId, new Dictionary<string, object>
            {
                { TelemetryEvents.Temperature, payload.Temperature }
            });

                // Send the device telemetry.
                await DeviceController.SendDeviceTelemetryAsync(telemetry, default);
            }
            else if(arg.ApplicationMessage.Topic == TopicNames.LightBulbToMyHome)
            {
                var payload = ParsePayload<LightbulbPayload>(arg.ApplicationMessage);
                await DeviceController.UpdatePropertyAsync(payload.ComponentId, "State", payload.On ? 1 : 0, default);
            }
        }
    }
}
