using MyHome.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
namespace MyHome.Simulator.Thermostat
{
    public class ThermostatMqttClientHandler : MqttClientHandler
    {
        private readonly IThermostatHandler Thermostat;
        public ThermostatMqttClientHandler(IMqttClient client, IConfiguration config, ILogger<MqttClientHandler> logger, IThermostatHandler thermostat) : base(client, config, logger)
        {
            Thermostat = thermostat;
        }

        protected override string Topic => TopicNames.MyHomeToThermostat;

        protected override Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            var payload = ParsePayload<ThermostatPayload>(arg.ApplicationMessage);
            Thermostat.SetTargetTemperature(payload.TargetTemperature);
            Log.LogInformation("New Target temperature has been set: {TargetTemperature}", payload.TargetTemperature);
            return Task.CompletedTask;
        }
    }
}
