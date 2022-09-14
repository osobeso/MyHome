/**
* Azure IOT Central Sample
* DevDiv Hacking Sessions 2022
*/
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MyHome.Simulator.Thermostat;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, serviceCollection) =>
    {
        serviceCollection.AddSingleton<IMqttClient>(new MqttFactory().CreateMqttClient());
        serviceCollection.AddSingleton<IThermostatHandler, ThermostatHandler>();
        serviceCollection.AddHostedService<ThermostatMqttClientHandler>();
        serviceCollection.AddHostedService<ThermostatService>();
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