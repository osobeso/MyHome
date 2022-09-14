namespace MyHome.Web.Shared
{
    public class GetDeviceResponse
    {
        public bool Registered { get; set; } = false;
        public DeviceModel? Device { get; set; }
    }
}
