using MyHome.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
namespace MyHome.Simulator.Thermostat
{
    public class ThermostatService : BackgroundService
    {
        private readonly IMqttClient MqttClient;
        private readonly ILogger Log;
        private readonly IThermostatHandler Thermostat;
        public ThermostatService(IMqttClient mqttClient, ILogger<ThermostatService> logger, IThermostatHandler thermostat)
        {
            MqttClient = mqttClient;
            Log = logger;
            Thermostat = thermostat;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var factory = new MqttFactory();
                var builder = factory.CreateApplicationMessageBuilder();
                var message = builder.WithTopic(TopicNames.ThermostatToMyHome)
                    .WithPayload(GetCurrentPayload()).Build();
                var publishResult = await MqttClient.PublishAsync(message, stoppingToken);
                if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    Log.LogError("Connection succeeded but could not send message to broker. Reason Code: {ReasonCode}", publishResult.ReasonCode);
                }
                Log.LogDebug("Send Thermostat status via MQTT.");
            }
        }

        private string GetCurrentPayload()
        {
            var payload = Thermostat.GetPayload();
            return JsonConvert.SerializeObject(payload);
        }
    }
}
