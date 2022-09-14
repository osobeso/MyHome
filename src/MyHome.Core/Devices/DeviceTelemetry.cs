namespace MyHome.Core.Devices
{
    public record DeviceTelemetry(string? ComponentId, Dictionary<string, object> Properties);
}
