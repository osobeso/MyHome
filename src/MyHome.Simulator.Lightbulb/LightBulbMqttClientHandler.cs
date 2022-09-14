namespace MyHome.Simulator.LightBulb
{
    using MyHome.Shared;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using MQTTnet.Client;
    using System.Threading.Tasks;
    using MQTTnet;
    using Newtonsoft.Json;

    public class LightBulbMqttClientHandler : MqttClientHandler
    {
        private readonly LightBulbHandler LightBulb;
        private readonly string ComponentId;

        public LightBulbMqttClientHandler(IMqttClient client, IConfiguration config, ILogger<MqttClientHandler> logger, LightBulbHandler lightBulb)
            : base(client, config, logger)
        {
            LightBulb = lightBulb;
            ComponentId = config.GetValue<string>("Device:ComponentId");
        }

        protected override string Topic => TopicNames.MyHomeToLightBulb;

        protected override async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            LightBulb.SwitchState();
            await SendUpdateMessageToMyHome();
        }

        private async Task SendUpdateMessageToMyHome()
        {
            var factory = new MqttFactory();
            var builder = factory.CreateApplicationMessageBuilder();
            var message = builder.WithTopic(TopicNames.LightBulbToMyHome)
                .WithPayload(GetPayload()).Build();
            var publishResult = await MqttClient.PublishAsync(message, default);
            if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
            {
                Log.LogError("Connection succeeded but could not send message to broker. Reason Code: {ReasonCode}", publishResult.ReasonCode);
            }
            Log.LogDebug("Send Lightbulb status via MQTT.");
        }

        private string GetPayload()
        {
            var payload = new LightbulbPayload(ComponentId, LightBulb.GetState());
            return JsonConvert.SerializeObject(payload!);
        }
    }
}
