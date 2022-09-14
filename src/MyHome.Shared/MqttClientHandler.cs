using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using System.Text;

namespace MyHome.Shared
{
    public abstract class MqttClientHandler : BackgroundService
    {
        protected readonly IMqttClient MqttClient;
        protected readonly ILogger Log;
        private bool _subscribed = false;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly int Port;
        protected abstract string Topic { get; }
        private readonly string ServerAddress;
        public MqttClientHandler(IMqttClient client, IConfiguration config, ILogger<MqttClientHandler> logger)
        {
            MqttClient = client;
            Log = logger;
            Port = config.GetValue<int>("Mqtt:Port");
            ServerAddress = config.GetValue<string>("Mqtt:ServerAddress");
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await HandleClientConnection(stoppingToken);
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Mqtt client handler stopped. Message: {Message}", ex.Message);
            }
        }

        private async Task HandleClientConnection(CancellationToken cancellationToken)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            do
            {
                // Use semaphore to avoid multithreading errors.
                await _semaphore.WaitAsync();
                try
                {
                    // If already connected don't need to connect again.
                    if (MqttClient.IsConnected)
                    {
                        continue;
                    }

                    // If not already subscribed, subscribe to events.
                    if (!_subscribed)
                    {
                        MqttClient.ConnectedAsync += MqttClient_ConnectedAsync;
                        MqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
                        MqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
                        Log.LogDebug("Successfully subscribed to MQTT events.");
                        _subscribed = true;
                    }

                    // Attempt connection to MQTT.
                    Log.LogInformation("Stablishing connection with MQTT broker.");
                    Log.LogDebug("Broker Endpoint: {ServerAddress}:{Port} Topic: {Topic}", ServerAddress, Port, Topic);

                    // Create MQTT Client Options
                    var options = new MqttClientOptionsBuilder()
                        .WithTcpServer(ServerAddress, Port)
                        .Build();

                    // Create Topic Subscription Options
                    var subscriptionOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(f =>
                        {
                            f.WithTopic(Topic);
                        }).Build();

                    await MqttClient.ConnectAsync(options, cancellationToken);
                    await MqttClient.SubscribeAsync(subscriptionOptions, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, "Could not connect to Mqtt client. Will reattempt in 15 seconds. Message: {Message}", ex.Message);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }

        protected TPayload ParsePayload<TPayload>(MqttApplicationMessage message)
        {
            var payloadStr = Encoding.UTF8.GetString(message.Payload);
            return JsonConvert.DeserializeObject<TPayload>(payloadStr)!;
        }

        protected abstract Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg);

        private Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            Log.LogInformation("Mqtt client disconnected. Reason: {Reason}", arg.Reason);
            Log.LogInformation("Service will re-attempt connection until it succeeds.");
            return Task.CompletedTask;
        }

        private Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            Log.LogInformation("Connected succesfully to MQTT Client.");
            return Task.CompletedTask;
        }
    }

    public record AirConditioningPayload(float Temperature, float TargetTemperature);
}
