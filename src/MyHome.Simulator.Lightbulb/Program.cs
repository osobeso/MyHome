/**
* Azure IOT Central Sample
* DevDiv Hacking Sessions 2022
*/
using MyHome.Simulator.LightBulb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, serviceCollection) =>
    {
        serviceCollection.AddSingleton<IMqttClient>(new MqttFactory().CreateMqttClient());
        serviceCollection.AddHostedService<LightBulbMqttClientHandler>();
        serviceCollection.AddSingleton<LightBulbHandler>();
        serviceCollection.AddLogging();
    })
    .ConfigureLogging((context, builder) =>
    {
        builder
        .AddConsole()
        .AddDebug();
    })
    .Build();

await host.RunAsync();