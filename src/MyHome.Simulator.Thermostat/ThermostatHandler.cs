using MyHome.Shared;
using Microsoft.Extensions.Configuration;

namespace MyHome.Simulator.Thermostat
{
    public interface IThermostatHandler
    {
        void SetTargetTemperature(double target);
        ThermostatPayload GetPayload();
    }
    public class ThermostatHandler : IThermostatHandler
    {
        private double Temperature = 60;
        private double TargetTemperature = 60;
        private readonly string ComponentId;

        public ThermostatHandler(IConfiguration config)
        {
            ComponentId = config.GetValue<string>("Device:ComponentId");
        }

        public ThermostatPayload GetPayload()
        {
            Temperature = ReadTemperature();
            return new ThermostatPayload(ComponentId, Temperature, TargetTemperature);
        }

        private double ReadTemperature()
        {
            var diff = TargetTemperature - Temperature;
            return Temperature + diff * 0.01f;
        }

        public void SetTargetTemperature(double target)
        {
            TargetTemperature = target;
        }
    }
}
