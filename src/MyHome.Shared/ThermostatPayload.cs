namespace MyHome.Shared
{
    public record ThermostatPayload(string ComponentId, double Temperature, double TargetTemperature);
}
